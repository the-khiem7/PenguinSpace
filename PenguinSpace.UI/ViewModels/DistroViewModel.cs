using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using PenguinSpace.Disk;
using PenguinSpace.UI.Services;

namespace PenguinSpace.UI.ViewModels;

public partial class DistroViewModel : ObservableObject
{
    private readonly IDistroService _distroService;
    private readonly IDiskService _diskService;
    private readonly IDialogService _dialogService;
    private readonly Action<string> _setStatus;
    private readonly Func<Task> _refreshList;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _state;

    [ObservableProperty]
    private int _wslVersion;

    [ObservableProperty]
    private string? _vhdxPath;

    [ObservableProperty]
    private long? _vhdxSizeBytes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanExecuteCommands))]
    private bool _isOperationRunning;

    [ObservableProperty]
    private string _progressMessage = string.Empty;

    public bool CanExecuteCommands => !IsOperationRunning;

    public string DisplaySize
    {
        get
        {
            if (VhdxSizeBytes is null) return "N/A";
            var bytes = VhdxSizeBytes.Value;
            if (bytes >= 1_073_741_824)
                return $"{bytes / 1_073_741_824.0:F1} GB";
            return $"{bytes / 1_048_576.0:F1} MB";
        }
    }

    public DistroViewModel(
        WslDistro distro,
        IDistroService distroService,
        IDiskService diskService,
        IDialogService dialogService,
        Action<string> setStatus,
        Func<Task> refreshList)
    {
        _name = distro.Name;
        _state = distro.State;
        _wslVersion = distro.WslVersion;
        _vhdxPath = distro.VhdxPath;
        _vhdxSizeBytes = distro.VhdxSizeBytes;
        _distroService = distroService;
        _diskService = diskService;
        _dialogService = dialogService;
        _setStatus = setStatus;
        _refreshList = refreshList;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task Reset()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Reset Distro",
            $"Bạn có chắc muốn reset '{Name}'? Toàn bộ dữ liệu trong distro sẽ bị xoá.");
        if (!confirmed) return;

        IsOperationRunning = true;
        ProgressMessage = "Đang reset...";
        _setStatus($"Đang reset {Name}...");

        try
        {
            var progress = new Progress<OperationProgress>(p =>
            {
                ProgressMessage = p.StepDescription;
                _setStatus($"{Name}: {p.StepDescription} ({p.CurrentStep}/{p.TotalSteps})");
            });

            var result = await _distroService.ResetDistroAsync(Name, progress);

            if (result.Success)
            {
                _setStatus($"Reset '{Name}' thành công.");
                await _refreshList();
            }
            else
            {
                _setStatus($"Reset '{Name}' thất bại: {result.ErrorMessage}");
            }
        }
        finally
        {
            IsOperationRunning = false;
            ProgressMessage = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task Repack()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Repack Distro",
            $"Bạn có chắc muốn repack '{Name}'? Quá trình này có thể mất nhiều thời gian. Không ngắt kết nối trong khi repack.");
        if (!confirmed) return;

        IsOperationRunning = true;
        ProgressMessage = "Đang repack...";
        _setStatus($"Đang repack {Name}...");

        try
        {
            var progress = new Progress<OperationProgress>(p =>
            {
                ProgressMessage = p.StepDescription;
                _setStatus($"{Name}: {p.StepDescription} ({p.CurrentStep}/{p.TotalSteps})");
            });

            var result = await _distroService.RepackDistroAsync(Name, progress);

            if (result.Success)
            {
                _setStatus($"Repack '{Name}' thành công.");
                await _refreshList();
            }
            else
            {
                _setStatus($"Repack '{Name}' thất bại: {result.ErrorMessage}");
            }
        }
        finally
        {
            IsOperationRunning = false;
            ProgressMessage = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task Compact()
    {
        if (VhdxPath is null)
        {
            _setStatus($"Không tìm thấy đường dẫn VHDX cho '{Name}'.");
            return;
        }

        IsOperationRunning = true;
        ProgressMessage = "Đang compact...";
        _setStatus($"Đang compact VHDX cho {Name}...");

        try
        {
            var progress = new Progress<OperationProgress>(p =>
            {
                ProgressMessage = p.StepDescription;
                _setStatus($"{Name}: {p.StepDescription} ({p.CurrentStep}/{p.TotalSteps})");
            });

            var result = await _diskService.CompactVhdxAsync(VhdxPath, progress);

            if (result.Success)
            {
                var before = FormatBytes(result.Data.Before);
                var after = FormatBytes(result.Data.After);
                var saved = FormatBytes(result.Data.Before - result.Data.After);
                _setStatus($"Compact '{Name}' thành công. Trước: {before} → Sau: {after} (tiết kiệm {saved})");

                // Refresh size
                VhdxSizeBytes = _diskService.GetVhdxSize(VhdxPath);
                OnPropertyChanged(nameof(DisplaySize));
            }
            else
            {
                _setStatus($"Compact '{Name}' thất bại: {result.ErrorMessage}");
            }
        }
        finally
        {
            IsOperationRunning = false;
            ProgressMessage = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private void OpenFolder()
    {
        if (VhdxPath is null)
        {
            _setStatus($"Không tìm thấy đường dẫn VHDX cho '{Name}'.");
            return;
        }

        var folder = Path.GetDirectoryName(VhdxPath);
        if (folder is null)
        {
            _setStatus($"Không xác định được thư mục cho '{Name}'.");
            return;
        }

        _diskService.OpenFolderInExplorer(folder);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824)
            return $"{bytes / 1_073_741_824.0:F1} GB";
        return $"{bytes / 1_048_576.0:F1} MB";
    }
}
