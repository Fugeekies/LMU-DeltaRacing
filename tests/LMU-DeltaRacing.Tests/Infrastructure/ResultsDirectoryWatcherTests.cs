using System.Collections.Concurrent;
using FluentAssertions;
using LMU_DeltaRacing.Application.Abstractions;
using LMU_DeltaRacing.Application.Models;
using LMU_DeltaRacing.Application.Services;
using LMU_DeltaRacing.Infrastructure.Parsing;
using LMU_DeltaRacing.Infrastructure.Persistence;
using LMU_DeltaRacing.Infrastructure.Watching;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMU_DeltaRacing.Tests.Infrastructure;

/// <summary>
/// Verifies dynamic imports from a watched LMU results directory.
/// </summary>
[TestClass]
public sealed class ResultsDirectoryWatcherTests
{
    private const string FixturePath = "assets/Results/2025_02_24_20_26_56-18R1.xml";

    /// <summary>
    /// Gets the MSTest context used to report load-test measurements.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Verifies that a file held with an exclusive lock is retried and imported after it is released.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_retries_a_locked_file_until_it_can_be_imported()
    {
        using var directory = new TemporaryDirectory();
        var resultPath = Path.Combine(directory.Path, "locked-result.xml");
        var fixtureContent = await File.ReadAllBytesAsync(GetFixturePath());
        var completion = new TaskCompletionSource<SessionImportResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using var watcher = CreateWatcher(directory.Path, completion.SetResult);

        using (var lockedFile = new FileStream(
                   resultPath,
                   FileMode.CreateNew,
                   FileAccess.Write,
                   FileShare.None))
        {
            await lockedFile.WriteAsync(fixtureContent);
            await lockedFile.FlushAsync();
            await watcher.StartAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(400));
            completion.Task.IsCompleted.Should().BeFalse();
        }

        var result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));

        result.Status.Should().Be(SessionImportStatus.Imported);
        watcher.ImportedSessionCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that malformed files are reported and do not stop subsequent valid imports.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_reports_invalid_xml_and_continues_importing_other_files()
    {
        using var directory = new TemporaryDirectory();
        var results = new ConcurrentQueue<SessionImportResult>();
        var completed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new InMemorySessionRepository();
        await using var watcher = CreateWatcher(directory.Path, result =>
        {
            results.Enqueue(result);
            if (results.Count == 2)
            {
                completed.TrySetResult();
            }
        }, repository);
        await watcher.StartAsync();

        await File.WriteAllTextAsync(Path.Combine(directory.Path, "invalid.xml"), "<not-valid-xml>");
        await results.WaitForCountAsync(1, TimeSpan.FromSeconds(10));
        File.Copy(GetFixturePath(), Path.Combine(directory.Path, "valid.xml"));

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        results.Should().ContainSingle(result => result.Status == SessionImportStatus.Failed);
        results.Should().ContainSingle(result => result.Status == SessionImportStatus.Imported);
        repository.GetAll().Should().ContainSingle();
        watcher.ImportedSessionCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that repeated file-system notifications for an imported path do not import it twice.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_does_not_import_a_file_twice_after_change_notifications()
    {
        using var directory = new TemporaryDirectory();
        var resultPath = Path.Combine(directory.Path, "single-result.xml");
        var fixtureContent = await File.ReadAllBytesAsync(GetFixturePath());
        await File.WriteAllBytesAsync(resultPath, fixtureContent);
        var results = new ConcurrentQueue<SessionImportResult>();
        var firstImport = new TaskCompletionSource<SessionImportResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new InMemorySessionRepository();
        await using var watcher = CreateWatcher(directory.Path, result =>
        {
            results.Enqueue(result);
            firstImport.TrySetResult(result);
        }, repository);
        await watcher.StartAsync();

        (await firstImport.Task.WaitAsync(TimeSpan.FromSeconds(10)))
            .Status.Should().Be(SessionImportStatus.Imported);
        await File.WriteAllBytesAsync(resultPath, fixtureContent);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        results.Should().ContainSingle();
        repository.GetAll().Should().ContainSingle();
    }

    /// <summary>
    /// Verifies that atomically moved result files are detected through rename notifications.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_imports_an_xml_file_renamed_into_the_watched_directory()
    {
        using var directory = new TemporaryDirectory();
        var resultPath = Path.Combine(directory.Path, "result.xml");
        var temporaryPath = Path.Combine(directory.Path, "result.xml.tmp");
        var result = new TaskCompletionSource<SessionImportResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new InMemorySessionRepository();
        await using var watcher = CreateWatcher(directory.Path, result.SetResult, repository);
        await watcher.StartAsync();

        File.Copy(GetFixturePath(), temporaryPath);
        File.Move(temporaryPath, resultPath);

        (await result.Task.WaitAsync(TimeSpan.FromSeconds(10)))
            .Status.Should().Be(SessionImportStatus.Imported);
        repository.GetAll().Should().ContainSingle();
    }

    /// <summary>
    /// Verifies that files handled by the startup batch are not imported again by the watcher scan.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_skips_files_already_imported_during_startup()
    {
        using var directory = new TemporaryDirectory();
        var resultPath = Path.Combine(directory.Path, "startup-result.xml");
        File.Copy(GetFixturePath(), resultPath);
        var results = new ConcurrentQueue<SessionImportResult>();
        var repository = new InMemorySessionRepository();
        using (var fixture = File.OpenRead(GetFixturePath()))
        {
            repository.Save(new XmlResultParser().Parse(fixture));
        }

        await using var watcher = CreateWatcher(directory.Path, results.Enqueue, repository);

        await watcher.StartAsync([resultPath]);
        await Task.Delay(TimeSpan.FromMilliseconds(400));

        results.Should().BeEmpty();
        repository.GetAll().Should().ContainSingle();
        watcher.ImportedSessionCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that fifty files created concurrently are all imported once.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_imports_fifty_concurrently_created_result_files()
    {
        using var directory = new TemporaryDirectory();
        const int fileCount = 50;
        var results = new ConcurrentQueue<SessionImportResult>();
        var allImportsCompleted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new InMemorySessionRepository();
        await using var watcher = CreateWatcher(directory.Path, result =>
        {
            results.Enqueue(result);
            if (results.Count == fileCount)
            {
                allImportsCompleted.TrySetResult();
            }
        }, repository);
        await watcher.StartAsync();
        var fixturePath = GetFixturePath();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await Task.Run(() => Parallel.For(
            0,
            fileCount,
            index => File.Copy(
                fixturePath,
                Path.Combine(directory.Path, $"result-{index:D2}.xml"))));

        await allImportsCompleted.Task.WaitAsync(TimeSpan.FromSeconds(30));
        stopwatch.Stop();

        results.Should().HaveCount(fileCount);
        results.Should().OnlyContain(result => result.Status == SessionImportStatus.Imported);
        repository.GetAll().Should().HaveCount(fileCount);
        watcher.ImportedSessionCount.Should().Be(fileCount);
        TestContext.WriteLine(
            $"Concurrent import load: {fileCount} XML files imported successfully in {stopwatch.Elapsed.TotalMilliseconds:F0} ms.");
    }

    private static ResultsDirectoryWatcher CreateWatcher(
        string directory,
        Action<SessionImportResult> onImportCompleted,
        InMemorySessionRepository? repository = null)
    {
        repository ??= new InMemorySessionRepository();
        var importer = CreateImporter(repository);
        var watcher = new ResultsDirectoryWatcher(
            directory,
            importer,
            NullLogger<ResultsDirectoryWatcher>.Instance);
        watcher.ImportCompleted += onImportCompleted;
        return watcher;
    }

    private static ISessionImportService CreateImporter(InMemorySessionRepository repository) =>
        new SessionImportService(
            new XmlResultParser(),
            repository,
            NullLogger<SessionImportService>.Instance);

    private static string GetFixturePath() => Path.Combine(AppContext.BaseDirectory, FixturePath);

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"lmu-watcher-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}

internal static class ConcurrentQueueAssertions
{
    public static async Task WaitForCountAsync<T>(
        this ConcurrentQueue<T> queue,
        int expectedCount,
        TimeSpan timeout)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (queue.Count < expectedCount && stopwatch.Elapsed < timeout)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(25));
        }

        queue.Count.Should().BeGreaterThanOrEqualTo(expectedCount);
    }
}
