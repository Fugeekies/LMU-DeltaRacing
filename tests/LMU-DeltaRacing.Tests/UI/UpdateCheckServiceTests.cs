using System.Net;
using System.Text;
using FluentAssertions;
using LMU_DeltaRacing.UI.Models;
using LMU_DeltaRacing.UI.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMU_DeltaRacing.Tests.UI;

/// <summary>
/// Verifies the startup check of the latest GitHub release, without any network access.
/// </summary>
[TestClass]
public sealed class UpdateCheckServiceTests
{
    [TestMethod]
    public async Task A_newer_release_is_reported_with_its_version()
    {
        var service = CreateService("0.1.0", Release("v0.2.0"));

        await service.CheckAsync(CancellationToken.None);

        service.Result.Should().Be(new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, "0.2.0"));
    }

    [TestMethod]
    [DataRow("0.2.0")]
    [DataRow("0.3.0")]
    public async Task The_same_or_an_older_release_is_up_to_date(string currentVersion)
    {
        var service = CreateService(currentVersion, Release("v0.2.0"));

        await service.CheckAsync(CancellationToken.None);

        service.Result.Status.Should().Be(UpdateCheckStatus.UpToDate);
    }

    [TestMethod]
    public async Task The_request_identifies_the_application_as_the_GitHub_api_requires()
    {
        var handler = new FakeHandler(Release("v0.2.0"));
        var service = new UpdateCheckService(new HttpClient(handler), "0.1.0", () => true, NullLogger<UpdateCheckService>.Instance);

        await service.CheckAsync(CancellationToken.None);

        var request = handler.Requests.Should().ContainSingle().Subject;
        request.RequestUri!.ToString().Should().Be("https://api.github.com/repos/Fugeekies/LMU-DeltaRacing/releases/latest");
        request.Headers.UserAgent.ToString().Should().Be("LMU-DeltaRacing/0.1.0");
    }

    [TestMethod]
    public async Task A_disabled_check_sends_nothing()
    {
        var handler = new FakeHandler(Release("v0.2.0"));
        var service = new UpdateCheckService(new HttpClient(handler), "0.1.0", () => false, NullLogger<UpdateCheckService>.Instance);

        await service.CheckAsync(CancellationToken.None);

        service.Result.Status.Should().Be(UpdateCheckStatus.Disabled);
        handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task A_development_build_sends_nothing()
    {
        var handler = new FakeHandler(Release("v0.2.0"));
        var service = new UpdateCheckService(new HttpClient(handler), "0.0.0-dev", () => true, NullLogger<UpdateCheckService>.Instance);

        await service.CheckAsync(CancellationToken.None);

        service.Result.Status.Should().Be(UpdateCheckStatus.DevelopmentBuild);
        handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task An_http_error_is_a_failed_check()
    {
        var service = CreateService("0.1.0", new HttpResponseMessage(HttpStatusCode.Forbidden));

        await service.CheckAsync(CancellationToken.None);

        service.Result.Status.Should().Be(UpdateCheckStatus.Failed);
    }

    [TestMethod]
    public async Task Being_offline_is_a_failed_check()
    {
        var handler = new FakeHandler(new HttpRequestException("No such host is known."));
        var service = new UpdateCheckService(new HttpClient(handler), "0.1.0", () => true, NullLogger<UpdateCheckService>.Instance);

        await service.CheckAsync(CancellationToken.None);

        service.Result.Status.Should().Be(UpdateCheckStatus.Failed);
    }

    [TestMethod]
    [DataRow("{ not json")]
    [DataRow("{}")]
    [DataRow("""{ "tag_name": "nightly" }""")]
    [DataRow("""{ "tag_name": 12 }""")]
    public async Task An_unusable_answer_is_a_failed_check(string body)
    {
        var service = CreateService("0.1.0", Json(body));

        await service.CheckAsync(CancellationToken.None);

        service.Result.Status.Should().Be(UpdateCheckStatus.Failed);
    }

    [TestMethod]
    public async Task The_end_of_the_check_is_announced()
    {
        var service = CreateService("0.1.0", Release("v0.2.0"));
        var notifications = 0;
        service.Changed += () => notifications++;

        await service.CheckAsync(CancellationToken.None);

        notifications.Should().Be(1);
    }

    [TestMethod]
    public async Task The_startup_notice_is_given_only_once_and_only_for_a_new_release()
    {
        var service = CreateService("0.1.0", Release("v0.2.0"));
        service.TryConsumeStartupNotice().Should().BeFalse("the check has not finished yet");

        await service.CheckAsync(CancellationToken.None);

        service.TryConsumeStartupNotice().Should().BeTrue();
        service.TryConsumeStartupNotice().Should().BeFalse("a page reload must not show the popup again");
    }

    [TestMethod]
    public async Task No_startup_notice_without_a_new_release()
    {
        var service = CreateService("0.2.0", Release("v0.2.0"));

        await service.CheckAsync(CancellationToken.None);

        service.TryConsumeStartupNotice().Should().BeFalse();
    }

    private static UpdateCheckService CreateService(string currentVersion, HttpResponseMessage response) =>
        new(new HttpClient(new FakeHandler(response)), currentVersion, () => true, NullLogger<UpdateCheckService>.Instance);

    private static HttpResponseMessage Release(string tag) => Json($$"""{ "tag_name": "{{tag}}" }""");

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? response;
        private readonly Exception? exception;

        public FakeHandler(HttpResponseMessage response) => this.response = response;

        public FakeHandler(Exception exception) => this.exception = exception;

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return exception is null ? Task.FromResult(response!) : Task.FromException<HttpResponseMessage>(exception);
        }
    }
}
