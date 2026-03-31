using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.UI.Services;

namespace PenguinSpace.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IWslService _wslService;
    private readonly IDistroService _distroService;
    private readonly IDockerService _dockerService;
    private readonly IDiskService _diskService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<DistroViewModel> _distros = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isOperationRunning;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng.";

    public MainWindowViewModel(
        IWslService wslService,
        IDistroService distroService,
        IDockerService dockerService,
        IDiskService diskService,
        IDialogService dialogService)
    {
        _wslService = wslService;
        _distroService = distroService;
        _dockerService = dockerService;
        _diskService = diskService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadDistrosAsync();

    [RelayCommand]
    private async Task ShutdownWsl()
    {
        IsOperationRunning = true;
        StatusMessage = "Đang shutdown WSL...";
        try
        {
            var result = await _wslService.ShutdownAsync();
            StatusMessage = result.Success
                ? "WSL đã shutdown thành công."
                : $"Shutdown WSL thất bại: {result.ErrorMessage}";

            if (result.Success)
                await LoadDistrosAsync();
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task DockerCleanup()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Docker Cleanup",
            "Bạn có chắc muốn xoá toàn bộ Docker images, containers và volumes không dùng đến?");
        if (!confirmed) return;

        IsOperationRunning = true;
        StatusMessage = "Đang dọn dẹp Docker...";
        try
        {
            var result = await _dockerService.PruneAsync();
            StatusMessage = result.Success
                ? $"Docker cleanup thành công. Đã thu hồi: {result.Data}"
                : $"Docker cleanup thất bại: {result.ErrorMessage}";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    public async Task LoadDistrosAsync()
    {
        IsLoading = true;
        StatusMessage = "Đang tải danh sách distro...";
        Distros.Clear();

        try
        {
            var result = await _wslService.ListDistrosAsync();

            if (!result.Success)
            {
                StatusMessage = $"Lỗi khi tải distro: {result.ErrorMessage}";
                return;
            }

            var distroList = result.Data ?? [];

            if (distroList.Count == 0)
            {
                StatusMessage = "Không tìm thấy WSL distro nào.";
                return;
            }

            var viewModels = new List<DistroViewModel>();

            foreach (var distro in distroList)
            {
                var vhdxPath = _diskService.FindVhdxPath(distro.Name);
                long? vhdxSize = vhdxPath is not null ? _diskService.GetVhdxSize(vhdxPath) : null;

                var enriched = distro with { VhdxPath = vhdxPath, VhdxSizeBytes = vhdxSize };

                var vm = new DistroViewModel(
                    enriched,
                    _distroService,
                    _diskService,
                    _dialogService,
                    msg => StatusMessage = msg,
                    LoadDistrosAsync);

                viewModels.Add(vm);
            }

            // Sort by VhdxSizeBytes descending, nulls last
            var sorted = viewModels
                .OrderByDescending(v => v.VhdxSizeBytes.HasValue ? 1 : 0)
                .ThenByDescending(v => v.VhdxSizeBytes ?? 0);

            foreach (var vm in sorted)
                Distros.Add(vm);

            StatusMessage = $"Tìm thấy {Distros.Count} distro.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
