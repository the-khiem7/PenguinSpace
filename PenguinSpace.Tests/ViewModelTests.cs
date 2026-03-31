using Moq;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using PenguinSpace.UI.Services;
using PenguinSpace.UI.ViewModels;

namespace PenguinSpace.Tests;

/// <summary>
/// Unit tests for ViewModels.
/// Validates: Requirements 1.4, 2.3, 2.4, 11.5
/// </summary>
public class ViewModelTests
{
    private static MainWindowViewModel CreateViewModel(
        Mock<IWslService>? wslMock = null,
        Mock<IDiskService>? diskMock = null,
        Mock<IDistroService>? distroMock = null,
        Mock<IDockerService>? dockerMock = null,
        Mock<IDialogService>? dialogMock = null)
    {
        return new MainWindowViewModel(
            (wslMock ?? new Mock<IWslService>()).Object,
            (distroMock ?? new Mock<IDistroService>()).Object,
            (dockerMock ?? new Mock<IDockerService>()).Object,
            (diskMock ?? new Mock<IDiskService>()).Object,
            (dialogMock ?? new Mock<IDialogService>()).Object);
    }

    // ── Test 1: LoadDistrosAsync populates Distros list ──────────────────────

    [Fact]
    public async Task LoadDistrosAsync_WithDistros_PopulatesDistrosList()
    {
        // Arrange
        var distros = new List<WslDistro>
        {
            new("Ubuntu", "Stopped", 2, null, null),
            new("Debian", "Stopped", 2, null, null),
        };

        var wslMock = new Mock<IWslService>();
        wslMock.Setup(s => s.ListDistrosAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new OperationResult<IReadOnlyList<WslDistro>>(true, distros, null));

        var diskMock = new Mock<IDiskService>();
        diskMock.Setup(s => s.FindVhdxPath(It.IsAny<string>())).Returns("/fake/path.vhdx");
        diskMock.Setup(s => s.GetVhdxSize(It.IsAny<string>())).Returns(1_073_741_824L);

        var vm = CreateViewModel(wslMock, diskMock);

        // Act
        await vm.LoadDistrosAsync();

        // Assert
        Assert.Equal(2, vm.Distros.Count);
    }

    // ── Test 2: LoadDistrosAsync sorts by VhdxSizeBytes descending ───────────

    [Fact]
    public async Task LoadDistrosAsync_SortsByVhdxSizeDescending()
    {
        // Arrange – 3 distros with sizes 500 MB, 2 GB, 1 GB
        var distros = new List<WslDistro>
        {
            new("Small",  "Stopped", 2, null, null),
            new("Large",  "Stopped", 2, null, null),
            new("Medium", "Stopped", 2, null, null),
        };

        var wslMock = new Mock<IWslService>();
        wslMock.Setup(s => s.ListDistrosAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new OperationResult<IReadOnlyList<WslDistro>>(true, distros, null));

        var diskMock = new Mock<IDiskService>();
        diskMock.Setup(s => s.FindVhdxPath("Small")).Returns("/small.vhdx");
        diskMock.Setup(s => s.FindVhdxPath("Large")).Returns("/large.vhdx");
        diskMock.Setup(s => s.FindVhdxPath("Medium")).Returns("/medium.vhdx");
        diskMock.Setup(s => s.GetVhdxSize("/small.vhdx")).Returns(524_288_000L);    // ~500 MB
        diskMock.Setup(s => s.GetVhdxSize("/large.vhdx")).Returns(2_147_483_648L);  // 2 GB
        diskMock.Setup(s => s.GetVhdxSize("/medium.vhdx")).Returns(1_073_741_824L); // 1 GB

        var vm = CreateViewModel(wslMock, diskMock);

        // Act
        await vm.LoadDistrosAsync();

        // Assert – largest first
        Assert.Equal("Large",  vm.Distros[0].Name);
        Assert.Equal("Medium", vm.Distros[1].Name);
        Assert.Equal("Small",  vm.Distros[2].Name);
    }

    // ── Test 3: Empty list sets "Không tìm thấy" message ────────────────────

    [Fact]
    public async Task LoadDistrosAsync_EmptyList_SetsEmptyMessage()
    {
        // Arrange
        var wslMock = new Mock<IWslService>();
        wslMock.Setup(s => s.ListDistrosAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new OperationResult<IReadOnlyList<WslDistro>>(
                   true, new List<WslDistro>(), null));

        var vm = CreateViewModel(wslMock);

        // Act
        await vm.LoadDistrosAsync();

        // Assert
        Assert.Contains("Không tìm thấy", vm.StatusMessage);
        Assert.Empty(vm.Distros);
    }

    // ── Test 4: Service failure sets error message ───────────────────────────

    [Fact]
    public async Task LoadDistrosAsync_ServiceFails_SetsErrorMessage()
    {
        // Arrange
        var wslMock = new Mock<IWslService>();
        wslMock.Setup(s => s.ListDistrosAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new OperationResult<IReadOnlyList<WslDistro>>(
                   false, null, "WSL not available"));

        var vm = CreateViewModel(wslMock);

        // Act
        await vm.LoadDistrosAsync();

        // Assert
        Assert.Contains("Lỗi", vm.StatusMessage);
        Assert.Empty(vm.Distros);
    }

    // ── Test 5: DistroViewModel.DisplaySize returns "N/A" when size is null ──

    [Fact]
    public void DistroViewModel_DisplaySize_NullSize_ReturnsNA()
    {
        // Arrange
        var distro = new WslDistro("Ubuntu", "Stopped", 2, null, null);
        var vm = new DistroViewModel(
            distro,
            new Mock<IDistroService>().Object,
            new Mock<IDiskService>().Object,
            new Mock<IDialogService>().Object,
            _ => { },
            () => Task.CompletedTask);

        // Assert
        Assert.Equal("N/A", vm.DisplaySize);
    }

    // ── Test 6: DistroViewModel.DisplaySize formats GB correctly ─────────────

    [Fact]
    public void DistroViewModel_DisplaySize_GBSize_FormatsCorrectly()
    {
        // Arrange – exactly 2 GB
        var distro = new WslDistro("Ubuntu", "Stopped", 2, "/path.vhdx", 2_147_483_648L);
        var vm = new DistroViewModel(
            distro,
            new Mock<IDistroService>().Object,
            new Mock<IDiskService>().Object,
            new Mock<IDialogService>().Object,
            _ => { },
            () => Task.CompletedTask);

        // Assert
        Assert.Contains("GB", vm.DisplaySize);
    }
}
