using Moq;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using PenguinSpace.WSL;
using Serilog;

namespace PenguinSpace.Tests;

public class WslServiceTests
{
    #region ParseWslOutput — Standard output with multiple distros

    [Fact]
    public void ParseWslOutput_StandardOutput_ReturnsAllDistros()
    {
        // Arrange — typical `wsl -l -v` output
        var output = """
              NAME            STATE           VERSION
            * Ubuntu          Running         2
              Debian          Stopped         2
              docker-desktop  Stopped         2
            """;

        // Act
        var distros = WslService.ParseWslOutput(output);

        // Assert
        Assert.Equal(3, distros.Count);

        Assert.Equal("Ubuntu", distros[0].Name);
        Assert.Equal("Running", distros[0].State);
        Assert.Equal(2, distros[0].WslVersion);

        Assert.Equal("Debian", distros[1].Name);
        Assert.Equal("Stopped", distros[1].State);
        Assert.Equal(2, distros[1].WslVersion);

        Assert.Equal("docker-desktop", distros[2].Name);
        Assert.Equal("Stopped", distros[2].State);
        Assert.Equal(2, distros[2].WslVersion);
    }

    #endregion

    #region ParseWslOutput — Unicode null characters (UTF-16 simulation)

    [Fact]
    public void ParseWslOutput_UnicodeNullCharacters_ParsesCorrectly()
    {
        // Arrange — simulate UTF-16 output with \0 between characters
        var raw = "  NAME            STATE           VERSION\n* Ubuntu          Running         2\n  Debian          Stopped         2\n";
        var withNulls = string.Join("\0", raw.Select(c => c.ToString()));

        // Act
        var distros = WslService.ParseWslOutput(withNulls);

        // Assert
        Assert.Equal(2, distros.Count);
        Assert.Equal("Ubuntu", distros[0].Name);
        Assert.Equal("Debian", distros[1].Name);
    }

    #endregion

    #region ParseWslOutput — Empty output (no distros)

    [Fact]
    public void ParseWslOutput_EmptyString_ReturnsEmptyList()
    {
        var distros = WslService.ParseWslOutput("");
        Assert.Empty(distros);
    }

    [Fact]
    public void ParseWslOutput_WhitespaceOnly_ReturnsEmptyList()
    {
        var distros = WslService.ParseWslOutput("   \n  \n  ");
        Assert.Empty(distros);
    }

    [Fact]
    public void ParseWslOutput_HeaderOnly_ReturnsEmptyList()
    {
        var output = "  NAME            STATE           VERSION\n";
        var distros = WslService.ParseWslOutput(output);
        Assert.Empty(distros);
    }

    #endregion

    #region ListDistrosAsync — Command failure

    [Fact]
    public async Task ListDistrosAsync_CommandFails_ReturnsFailedResult()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync("wsl", "-l -v", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new CommandResult(1, "", "WSL is not installed", TimeSpan.FromMilliseconds(50)));

        var mockLogger = new Mock<ILogger>();
        var sut = new WslService(mockRunner.Object, mockLogger.Object);

        // Act
        var result = await sut.ListDistrosAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Contains("WSL is not installed", result.ErrorMessage);
    }

    #endregion
}
