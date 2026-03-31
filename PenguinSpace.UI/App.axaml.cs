using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PenguinSpace.Disk;
using PenguinSpace.Docker;
using PenguinSpace.Infrastructure;
using PenguinSpace.Infrastructure.Logging;
using PenguinSpace.UI.Services;
using PenguinSpace.UI.ViewModels;
using PenguinSpace.UI.Views;
using PenguinSpace.WSL;

namespace PenguinSpace.UI;

public class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = LoggingConfiguration.CreateLogger();
            var commandRunner = new CommandRunner(logger);
            var wslService = new WslService(commandRunner, logger);
            var distroService = new DistroService(commandRunner, logger);
            var dockerService = new DockerService(commandRunner, logger);
            var diskService = new DiskService(commandRunner, logger);
            var dialogService = new DialogService();

            var viewModel = new MainWindowViewModel(
                wslService,
                distroService,
                dockerService,
                diskService,
                dialogService);

            var mainWindow = new MainWindow(viewModel);
            desktop.MainWindow = mainWindow;

            // Load distros on startup
            _ = viewModel.LoadDistrosAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
