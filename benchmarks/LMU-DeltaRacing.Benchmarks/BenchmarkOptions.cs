namespace LMU_DeltaRacing.Benchmarks;

internal sealed record BenchmarkOptions(string ReportPath, int Years, bool ShowHelp)
{
    public const string Usage =
        "Usage: LMU-DeltaRacing.Benchmarks [--report <path>] [--years <2|5|10>] [--help]";

    public static BenchmarkOptions Parse(string[] args)
    {
        var reportPath = Path.Combine("artifacts", "performance-report.md");
        var years = 10;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--help":
                case "-h":
                    return new BenchmarkOptions(reportPath, years, ShowHelp: true);
                case "--report" when index + 1 < args.Length:
                    reportPath = args[++index];
                    break;
                case "--years" when index + 1 < args.Length:
                    if (!int.TryParse(args[++index], out years) || years is not (2 or 5 or 10))
                    {
                        throw new ArgumentException("--years must be one of 2, 5, or 10.");
                    }

                    break;
                default:
                    throw new ArgumentException($"Unknown or incomplete argument: {args[index]}");
            }
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        return new BenchmarkOptions(reportPath, years, ShowHelp: false);
    }
}
