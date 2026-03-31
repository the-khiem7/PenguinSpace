using Moq;
using PenguinSpace.Infrastructure;
using Serilog;

namespace PenguinSpace.Tests;

public class CommandRunnerTests
{
    private readonly CommandRunner _sut;

    public CommandRunnerTests()
    {
        var mockLogger = new Mock<ILogger>();
        _sut = new CommandRunner(mockLogger.Object);
    }

    [Fact]
    public async Task RunAsync_CapturesStdOut()
    {
        // Arrange & Act
        var result = await _sut.RunAsync("cmd", "/c echo hello");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello", result.StdOut);
    }

    [Fact]
    public async Task RunAsync_CapturesStdErr()
    {
        // Arrange & Act
        var result = await _sut.RunAsync("cmd", "/c echo error>&2");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("error", result.StdErr);
    }

    [Fact]
    public async Task RunAsync_CapturesExitCode()
    {
        // Arrange & Act
        var result = await _sut.RunAsync("cmd", "/c exit 42");

        // Assert
        Assert.Equal(42, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_TimesOut_ReturnsExitCodeMinusOneAndTimeoutMessage()
    {
        // Arrange — use a 2-second timeout with a command that takes much longer
        // Act
        var result = await _sut.RunAsync("ping", "-n 30 127.0.0.1", timeoutSeconds: 2);

        // Assert
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("timed out", result.StdErr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_CancellationToken_ReturnsExitCodeMinusOneAndCancelledMessage()
    {
        // Arrange — cancel after 1 second
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act
        var result = await _sut.RunAsync("ping", "-n 30 127.0.0.1", cancellationToken: cts.Token);

        // Assert
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("cancelled", result.StdErr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_RecordsDuration()
    {
        // Act
        var result = await _sut.RunAsync("cmd", "/c echo hi");

        // Assert
        Assert.True(result.Duration > TimeSpan.Zero);
    }
}
