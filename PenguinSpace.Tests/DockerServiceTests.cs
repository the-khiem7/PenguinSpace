using Moq;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using PenguinSpace.Docker;
using Serilog;

namespace PenguinSpace.Tests;

public class DockerServiceTests
{
    private readonly Mock<ICommandRunner> _mockRunner;
    private readonly Mock<ILogger> _mockLogger;
    private readonly DockerService _sut;

    public DockerServiceTests()
    {
        _mockRunner = new Mock<ICommandRunner>();
        _mockLogger = new Mock<ILogger>();
        _sut = new DockerService(_mockRunner.Object, _mockLogger.Object);
    }

    private static CommandResult Success(string stdout = "") =>
        new(0, stdout, "", TimeSpan.Zero);

    private static CommandResult Failure(string stderr = "error") =>
        new(1, "", stderr, TimeSpan.Zero);

    [Fact]
    public void ParseReclaimedSpace_ValidOutput_ReturnsReclaimedValue()
    {
        var output = "Deleted Images:\nsha256:abc\n\nTotal reclaimed space: 2.5GB\n";
        var result = DockerService.ParseReclaimedSpace(output);
        Assert.Equal("2.5GB", result);
    }

    [Fact]
    public void ParseReclaimedSpace_NoReclaimedLine_ReturnsZero()
    {
        var result = DockerService.ParseReclaimedSpace("Some output without the line");
        Assert.Equal("0B", result);
    }

    [Fact]
    public void ParseReclaimedSpace_EmptyOutput_ReturnsZero()
    {
        Assert.Equal("0B", DockerService.ParseReclaimedSpace(""));
    }

    [Fact]
    public async Task PruneAsync_Success_ReturnsReclaimedSpace()
    {
        var stdout = "Deleted Images:\n...\nTotal reclaimed space: 1.2GB\n";
        _mockRunner
            .Setup(r => r.RunAsync("docker", "system prune -a --volumes -f",
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(Success(stdout));

        var result = await _sut.PruneAsync();

        Assert.True(result.Success);
        Assert.Equal("1.2GB", result.Data);
    }

    [Fact]
    public async Task PruneAsync_DaemonNotRunning_ReturnsDaemonError()
    {
        _mockRunner
            .Setup(r => r.RunAsync("docker", "system prune -a --volumes -f",
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(Failure("Cannot connect to the Docker daemon"));

        var result = await _sut.PruneAsync();

        Assert.False(result.Success);
        Assert.Contains("Docker daemon", result.ErrorMessage);
    }

    [Fact]
    public async Task PruneAsync_CommandFails_ReturnsErrorMessage()
    {
        _mockRunner
            .Setup(r => r.RunAsync("docker", "system prune -a --volumes -f",
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(Failure("permission denied"));

        var result = await _sut.PruneAsync();

        Assert.False(result.Success);
        Assert.Contains("permission denied", result.ErrorMessage);
    }
}
