using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using Serilog;

namespace PenguinSpace.WSL;

public class DistroService : IDistroService
{
    private readonly ICommandRunner _commandRunner;
    private readonly ILogger _logger;

    public DistroService(ICommandRunner commandRunner, ILogger logger)
    {
        _commandRunner = commandRunner;
        _logger = logger;
    }

    public async Task<OperationResult<bool>> ResetDistroAsync(
        string distroName,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.Information("Starting reset for distro '{DistroName}'", distroName);

        try
        {
            // Step 1: Unregister
            progress?.Report(new OperationProgress("Unregistering distro...", 1, 2));
            _logger.Information("Unregistering distro '{DistroName}'", distroName);

            var unregisterResult = await _commandRunner.RunAsync("wsl", $"--unregister {distroName}", ct);

            if (unregisterResult.ExitCode != 0)
            {
                var error = string.IsNullOrWhiteSpace(unregisterResult.StdErr)
                    ? $"wsl --unregister {distroName} failed with exit code {unregisterResult.ExitCode}"
                    : unregisterResult.StdErr.Trim();

                _logger.Error("Unregister failed for '{DistroName}': {Error}", distroName, error);
                return new OperationResult<bool>(false, false, $"Unregister failed: {error}");
            }

            // Step 2: Install
            progress?.Report(new OperationProgress("Installing distro...", 2, 2));
            _logger.Information("Installing distro '{DistroName}'", distroName);

            var installResult = await _commandRunner.RunAsync("wsl", $"--install {distroName}", ct);

            if (installResult.ExitCode != 0)
            {
                var error = string.IsNullOrWhiteSpace(installResult.StdErr)
                    ? $"wsl --install {distroName} failed with exit code {installResult.ExitCode}"
                    : installResult.StdErr.Trim();

                _logger.Error("Install failed for '{DistroName}': {Error}", distroName, error);
                return new OperationResult<bool>(false, false, $"Install failed: {error}");
            }

            _logger.Information("Reset completed successfully for distro '{DistroName}'", distroName);
            return new OperationResult<bool>(true, true, null);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while resetting distro '{DistroName}'", distroName);
            return new OperationResult<bool>(false, false, ex.Message);
        }
    }

    public async Task<OperationResult<bool>> RepackDistroAsync(
        string distroName,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.Information("Starting repack for distro '{DistroName}'", distroName);

        var tempFile = Path.Combine(Path.GetTempPath(), $"{distroName}.tar");

        try
        {
            // Step 1: Export
            progress?.Report(new OperationProgress("Exporting distro...", 1, 3));
            _logger.Information("Exporting distro '{DistroName}' to '{TempFile}'", distroName, tempFile);

            var exportResult = await _commandRunner.RunAsync("wsl", $"--export {distroName} \"{tempFile}\"", ct);

            if (exportResult.ExitCode != 0)
            {
                var error = string.IsNullOrWhiteSpace(exportResult.StdErr)
                    ? $"wsl --export failed with exit code {exportResult.ExitCode}"
                    : exportResult.StdErr.Trim();

                _logger.Error("Export failed for '{DistroName}': {Error}", distroName, error);
                return new OperationResult<bool>(false, false, $"Export failed: {error}");
            }

            // Step 2: Unregister
            progress?.Report(new OperationProgress("Unregistering distro...", 2, 3));
            _logger.Information("Unregistering distro '{DistroName}'", distroName);

            var unregisterResult = await _commandRunner.RunAsync("wsl", $"--unregister {distroName}", ct);

            if (unregisterResult.ExitCode != 0)
            {
                var error = string.IsNullOrWhiteSpace(unregisterResult.StdErr)
                    ? $"wsl --unregister failed with exit code {unregisterResult.ExitCode}"
                    : unregisterResult.StdErr.Trim();

                _logger.Error("Unregister failed for '{DistroName}': {Error}. Export file kept at '{TempFile}'",
                    distroName, error, tempFile);
                return new OperationResult<bool>(false, false, $"Unregister failed: {error}. Export file kept at: {tempFile}");
            }

            // Step 3: Import
            progress?.Report(new OperationProgress("Importing distro...", 3, 3));
            var installPath = GetDefaultInstallPath(distroName);
            _logger.Information("Importing distro '{DistroName}' from '{TempFile}' to '{InstallPath}'",
                distroName, tempFile, installPath);

            var importResult = await _commandRunner.RunAsync(
                "wsl", $"--import {distroName} \"{installPath}\" \"{tempFile}\"", ct);

            if (importResult.ExitCode != 0)
            {
                var error = string.IsNullOrWhiteSpace(importResult.StdErr)
                    ? $"wsl --import failed with exit code {importResult.ExitCode}"
                    : importResult.StdErr.Trim();

                _logger.Error("Import failed for '{DistroName}': {Error}. Export file kept at '{TempFile}'",
                    distroName, error, tempFile);
                return new OperationResult<bool>(false, false, $"Import failed: {error}. Export file kept at: {tempFile}");
            }

            // Success — delete temp file
            try
            {
                File.Delete(tempFile);
                _logger.Information("Deleted temp export file '{TempFile}'", tempFile);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to delete temp export file '{TempFile}'", tempFile);
            }

            _logger.Information("Repack completed successfully for distro '{DistroName}'", distroName);
            return new OperationResult<bool>(true, true, null);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while repacking distro '{DistroName}'", distroName);
            return new OperationResult<bool>(false, false, ex.Message);
        }
    }

    internal static string GetDefaultInstallPath(string distroName)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "WSL", distroName);
    }
}
