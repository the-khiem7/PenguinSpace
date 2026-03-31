using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.Disk;
using PenguinSpace.Docker;
using PenguinSpace.Infrastructure;
using PenguinSpace.Infrastructure.Logging;
using PenguinSpace.UI.Services;
using PenguinSpace.UI.ViewModels;
using PenguinSpace.UI.Views;
using PenguinSpace.WSL;
using Serilog;

namespace PenguinSpace.UI;

public class App : Avalonia.Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow(viewModel);
            desktop.MainWindow = mainWindow;

            _ = viewModel.LoadDistrosAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Logging
        var logger = LoggingConfiguration.CreateLogger();
        services.AddSingleton<ILogger>(logger);

        // Infrastructure
        services.AddSingleton<ICommandRunner, CommandRunner>();

        // Services
        services.AddSingleton<IWslService, WslService>();
        services.AddSingleton<IDistroService, DistroService>();
        services.AddSingleton<IDockerService, DockerService>();
        services.AddSingleton<IDiskService, DiskService>();
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
