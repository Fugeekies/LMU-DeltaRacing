using LMU_DeltaRacing.Benchmarks;
using LMU_DeltaRacing.Infrastructure.Parsing;

var options = BenchmarkOptions.Parse(args);
if (options.ShowHelp)
{
    Console.WriteLine(BenchmarkOptions.Usage);
    return 0;
}

var report = new LoadBenchmarkRunner(new XmlResultParser()).Run(options);
var reportPath = Path.GetFullPath(options.ReportPath);
Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
File.WriteAllText(reportPath, report);
Console.WriteLine($"Rapport de performance écrit dans : {reportPath}");
return 0;
