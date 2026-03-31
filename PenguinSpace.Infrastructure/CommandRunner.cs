using System.Diagnostics;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using Serilog;

namespace PenguinSpace.Infrastructure;

public class CommandRunner : ICommandRunner
{
    private readonly ILogger _logger;

    public CommandRunner(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<CommandResult> RunAsync(
        string command,
        string arguments,
        CancellationToken cancellationToken = default,
        int timeoutSeconds = 300)
    {
        _logger.Information("Starting command: {Command} {Arguments}", command, arguments);
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var stdOutBuilder = new System.Text.StringBuilder();
        var stdErrBuilder = new System.Text.StringBuilder();
        var stdOutTcs = new TaskCompletionSource<bool>();
        var stdErrTcs = new TaskCompletionSource<bool>();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stdOutBuilder.AppendLine(e.Data);
            else
                stdOutTcs.TrySetResult(true);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stdErrBuilder.AppendLine(e.Data);
            else
                stdErrTcs.TrySetResult(true);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                KillProcess(process);

                stopwatch.Stop();
                var duration = stopwatch.Elapsed;
                var endTime = DateTime.UtcNow;

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Warning(
                        "Command cancelled by user: {Command} {Arguments} | Duration: {Duration}ms",
                        command, arguments, duration.TotalMilliseconds);

                    return new CommandResult(-1, stdOutBuilder.ToString(), "Command cancelled by user.", duration);
                }

                _logger.Warning(
                    "Command timed out after {Timeout}s: {Command} {Arguments} | Duration: {Duration}ms",
                    timeoutSeconds, command, arguments, duration.TotalMilliseconds);

                return new CommandResult(-1, stdOutBuilder.ToString(),
                    $"Command timed out after {timeoutSeconds} seconds.", duration);
            }

            // Wait for async output streams to finish
            await Task.WhenAll(stdOutTcs.Task, stdErrTcs.Task).ConfigureAwait(false);

            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;

            var result = new CommandResult(
                process.ExitCode,
                stdOutBuilder.ToString(),
                stdErrBuilder.ToString(),
                elapsed);

            _logger.Information(
                "Command completed: {Command} {Arguments} | ExitCode: {ExitCode} | Duration: {Duration}ms",
                command, arguments, result.ExitCode, elapsed.TotalMilliseconds);

            if (result.ExitCode != 0)
            {
                _logger.Warning(
                    "Command returned non-zero exit code: {ExitCode} | StdErr: {StdErr}",
                    result.ExitCode, result.StdErr);
            }

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.Error(ex,
                "Command failed with exception: {Command} {Arguments} | Duration: {Duration}ms",
                command, arguments, stopwatch.Elapsed.TotalMilliseconds);

            return new CommandResult(-1, string.Empty, ex.Message, stopwatch.Elapsed);
        }
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort kill — process may have already exited
        }
    }
}
