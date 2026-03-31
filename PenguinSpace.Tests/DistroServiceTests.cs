using Moq;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using PenguinSpace.WSL;
using Serilog;

namespace PenguinSpace.Tests;

public class DistroServiceTests
{
    private readonly Mock<ICommandRunner> _mockRunner;
    private readonly Mock<ILogger> _mockLogger;
    private readonly DistroService _sut;

    public DistroServiceTests()
    {
        _mockRunner = new Mock<ICommandRunner>();
        _mockLogger = new Mock<ILogger>();
        _sut = new DistroService(_mockRunner.Object, _mockLogger.Object);
    }

    private static CommandResult SuccessResult() =>
        new(0, "", "", TimeSpan.Zero);

    private static CommandResult FailureResult(string error = "error message") =>
        new(1, "", error, TimeSpan.Zero);

    #region ResetDistroAsync — unregister + install both succeed

    /// <summary>
    /// Validates: Requirements 3.2, 3.3
    /// Reset flow: unregister thành công → install thành công → returns Success=true
    /// </summary>
    [Fact]
    public async Task ResetDistroAsync_BothStepsSucceed_ReturnsSuccess()
    {
        // Arrange
        _mockRunner
            .Setup(r => r.RunAsync("wsl", "--unregister Ubuntu", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(SuccessResult());
        _mockRunner
            .Setup(r => r.RunAsync("wsl", "--install Ubuntu", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(SuccessResult());

        // Act
        var result = await _sut.ResetDistroAsync("Ubuntu");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Null(result.ErrorMessage);

        _mockRunner.Verify(
            r => r.RunAsync("wsl", "--unregister Ubuntu", It.IsAny<CancellationToken>(), It.IsAny<int>()),
            Times.Once);
        _mockRunner.Verify(
            r => r.RunAsync("wsl", "--install Ubuntu", It.IsAny<CancellationToken>(), It.IsAny<int>()),
            Times.Once);
    }

    #endregion

    #region ResetDistroAsync — unregister fails → install NOT called

    /// <summary>
    /// Validates: Requirements 3.2, 3.4
    /// Reset flow: unregister thất bại → returns Success=false, install is NOT called
    /// </summary>
    [Fact]
    public async Task ResetDistroAsync_UnregisterFails_ReturnsFailureAndSkipsInstall()
    {
        // Arrange
        _mockRunner
            .Setup(r => r.RunAsync("wsl", "--unregister Ubuntu", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(FailureResult("The distribution 'Ubuntu' was not found"));

        // Act
        var result = await _sut.ResetDistroAsync("Ubuntu");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unregister failed", result.ErrorMessage);

        _mockRunner.Verify(
            r => r.RunAsync("wsl", "--unregister Ubuntu", It.IsAny<CancellationToken>(), It.IsAny<int>()),
            Times.Once);
        _mockRunner.Verify(
            r => r.RunAsync("wsl", "--install Ubuntu", It.IsAny<CancellationToken>(), It.IsAny<int>()),
            Times.Never);
    }

    #endregion

    #region RepackDistroAsync — all 3 steps succeed

    /// <summary>
    /// Validates: Requirements 4.2, 4.3
    /// Repack flow: export → unregister → import all succeed → returns Success=true
    /// </summary>
    [Fact]
    public async Task RepackDistroAsync_AllStepsSucceed_ReturnsSuccess()
    {
        // Arrange
        var distroName = "Debian";
        var tempFile = Path.Combine(Path.GetTempPath(), $"{distroName}.tar");
        var installPath = DistroService.GetDefaultInstallPath(distroName);

        _mockRunner
            .Setup(r => r.RunAsync("wsl", $"--export {distroName} \"{tempFile}\"",
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(SuccessResult());
        _mockRunner
            .Setup(r => r.RunAsync("wsl", $"--unregister {distroName}",
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(SuccessResult());
        _mockRunner
            .Setup(r => r.RunAsync("wsl", $"--import {distroName} \"{installPath}\" \"{tempFile}\"",
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(SuccessResult());

        // Act
        var result = await _sut.RepackDistroAsync(distroName);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Null(result.ErrorMessage);

        _mockRunner.Verify(
            r => r.RunAsync("wsl", $"--export {distroName} \"{tempFile}\"",
                It.IsAny<CancellationToken>(), It.IsAny<int>()),
            Times.Once);
        _mockRunner.Verify(
            r => r.RunAsync("wsl", $"--unregister {distroName}",
                It.IsAny<CancellationToken>(), It.IsAny<int>()),
            Times.Once);
        _mockRunner.Verify(
            r => r.RunAsync("wsl", $"--import {distroName} \"{installPath}\" \"{tempFile}\"",
                It.IsAny<CancellationToken>(), It.IsAny<int>()),
            Times.Once);
    }

    #endregion

    #region RepackDistroAsync — import fails → returns error with temp file path

    /// <summary>
    /// Validates: Requirements 4.2, 4.4
    /// Repack flow: import thất bại → returns Success=false with error mentioning "Import failed" and temp file path
    /// </summary>
    [Fact]
    public async Task RepackDistroAsync_ImportFails_ReturnsFailureWithTempFilePath()
    {
        // Arrange
        var distroName = "Debian";
        var tempFile = Path.Combine(Path.GetTempPath(), $"{distroName}.tar");
        var installPath = DistroService.GetDefaultInstallPath(distroName);

        _mockRunner
            .Setup(r => r.RunAsync("wsl", $"--export {distroName} \"{tempFile}\"",
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(SuccessResult());
        _mockRunner
            .Setup(r => r.RunAsync("wsl", $"--unregister {distroName}",
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(SuccessResult());
        _mockRunner
            .Setup(r => r.RunAsync("wsl", $"--import {distroName} \"{installPath}\" \"{tempFile}\"",
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(FailureResult("Access denied"));

        // Act
        var result = await _sut.RepackDistroAsync(distroName);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Import failed", result.ErrorMessage);
        Assert.Contains(tempFile, result.ErrorMessage);
    }

    #endregion
}
