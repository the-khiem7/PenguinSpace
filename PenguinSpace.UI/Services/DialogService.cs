using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace PenguinSpace.UI.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow is null)
            return false;

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SizeToContent = SizeToContent.Height
        };

        var tcs = new TaskCompletionSource<bool>();

        var msgText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16, 16, 16, 8)
        };

        var yesBtn = new Button { Content = "Xác nhận", Width = 100, Margin = new Thickness(0, 0, 8, 0) };
        var noBtn = new Button { Content = "Huỷ", Width = 100 };

        yesBtn.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
        noBtn.Click += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(16, 8, 16, 16),
            Children = { yesBtn, noBtn }
        };

        dialog.Content = new StackPanel { Children = { msgText, btnPanel } };

        await dialog.ShowDialog(mainWindow);
        return await tcs.Task;
    }

    private static Window? GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}
