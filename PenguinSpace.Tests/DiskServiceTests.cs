using Moq;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using PenguinSpace.Disk;
using Serilog;

namespace PenguinSpace.Tests;

public class DiskServiceTests
{
    private readonly Mock<ICommandRunner> _mockRunner;
    private readonly Mock<ILogger> _mockLogger;
    private readonly DiskService _sut;

    public DiskServiceTests()
    {
        _mockRunner = new Mock<ICommandRunner>();
        _mockLogger = new Mock<ILogger>();
        // ForContext returns a new logger — mock it to return itself
        _mockLogger.Setup(l => l.ForContext<DiskService>()).Returns(_mockLogger.Object);
        _sut = new DiskService(_mockRunner.Object, _mockLogger.Object);
    }

    // ── GetVhdxSize ──────────────────────────────────────────────────────────

    [Fact]
    public void GetVhdxSize_FileExists_ReturnsLength()
    {
        // Create a real temp file to test against
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test content");
            var size = _sut.GetVhdxSize(tempFile);
            Assert.NotNull(size);
            Assert.True(size > 0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetVhdxSize_FileNotExist_ReturnsNull()
    {
        var result = _sut.GetVhdxSize(@"C:\nonexistent\path\ext4.vhdx");
        Assert.Null(result);
    }

    // ── FindVhdxPath ─────────────────────────────────────────────────────────

    [Fact]
    public void FindVhdxPath_DockerDesktopData_ChecksDockerPath()
    {
        // This is a path-resolution test — we just verify it doesn't throw
        // and returns null when Docker isn't installed (CI environment)
        var result = DiskService.FindVhdxPathStatic("docker-desktop-data");
        // Result is null if Docker not installed — that's valid
        Assert.True(result is null || File.Exists(result));
    }

    [Fact]
    public void FindVhdxPath_UnknownDistro_ReturnsNull()
    {
        var result = DiskService.FindVhdxPathStatic("this-distro-does-not-exist-xyz");
        Assert.Null(result);
    }

    // ── CompactVhdxAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CompactVhdxAsync_FileNotFound_ReturnsFailure()
    {
        var result = await _sut.CompactVhdxAsync(@"C:\nonexistent\ext4.vhdx");

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompactVhdxAsync_DiskpartFails_ReturnsFailure()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _mockRunner
                .Setup(r => r.RunAsync("diskpart", It.IsAny<string>(),
                    It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ReturnsAsync(new CommandResult(1, "", "Access denied", TimeSpan.Zero));

            var result = await _sut.CompactVhdxAsync(tempFile);

            Assert.False(result.Success);
            Assert.Contains("diskpart failed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CompactVhdxAsync_Success_ReturnsSizesBeforeAndAfter()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, new byte[1024]);

            _mockRunner
                .Setup(r => r.RunAsync("diskpart", It.IsAny<string>(),
                    It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ReturnsAsync(new CommandResult(0, "DiskPart successfully compacted the virtual disk file.", "", TimeSpan.Zero));

            var result = await _sut.CompactVhdxAsync(tempFile);

            Assert.True(result.Success);
            Assert.True(result.Data.Before > 0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
