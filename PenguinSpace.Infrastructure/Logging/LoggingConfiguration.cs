using Serilog;

namespace PenguinSpace.Infrastructure.Logging;

/// <summary>
/// Provides static configuration for Serilog logging.
/// </summary>
public static class LoggingConfiguration
{
    private const string LogFileTemplate = "logs/penguinspace-.log";
    private const string OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Message}{NewLine}{Exception}";
    private const int RetainedFileCountDays = 30;

    /// <summary>
    /// Creates a pre-configured Serilog <see cref="ILogger"/> that writes to a daily rolling file
    /// at <c>&lt;app_directory&gt;/logs/penguinspace-YYYYMMDD.log</c>, retaining files for 30 days.
    /// </summary>
    public static ILogger CreateLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: LogFileTemplate,
                outputTemplate: OutputTemplate,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetainedFileCountDays)
            .CreateLogger();
    }
}
