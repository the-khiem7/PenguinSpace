using System.Diagnostics;
using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using Serilog;

namespace PenguinSpace.Disk;

public class DiskService : IDiskService
{
    private readonly ICommandRunner _commandRunner;
    private readonly ILogger _logger;

    public DiskService(ICommandRunner commandRunner, ILogger logger)
    {
        _commandRunner = commandRunner;
        _logger = logger.ForContext<DiskService>();
    }

    public long? GetVhdxSize(string vhdxPath)
    {
        var fi = new FileInfo(vhdxPath);
        if (!fi.Exists)
        {
            _logger.Warning("VHDX file not found: {VhdxPath}", vhdxPath);
            return null;
        }
        return fi.Length;
    }

    public string? FindVhdxPath(string distroName) => FindVhdxPathStatic(distroName);

    /// <summary>
    /// Scans the filesystem to find the VHDX path for a given distro name.
    /// Checks %LOCALAPPDATA%\Packages\ and Docker-specific paths.
    /// </summary>
    internal static string? FindVhdxPathStatic(string distroName)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Check Docker-specific paths first
        if (distroName.Equals("docker-desktop-data", StringComparison.OrdinalIgnoreCase))
        {
            var dockerDataPath = Path.Combine(localAppData, "Docker", "wsl", "data", "ext4.vhdx");
            if (File.Exists(dockerDataPath))
                return dockerDataPath;
        }

        if (distroName.Equals("docker-desktop", StringComparison.OrdinalIgnoreCase))
        {
            var dockerDistroPath = Path.Combine(localAppData, "Docker", "wsl", "distro", "ext4.vhdx");
            if (File.Exists(dockerDistroPath))
                return dockerDistroPath;
        }

        // Scan %LOCALAPPDATA%\Packages\ for directories containing LocalState\ext4.vhdx
        var packagesDir = Path.Combine(localAppData, "Packages");
        if (Directory.Exists(packagesDir))
        {
            foreach (var dir in Directory.EnumerateDirectories(packagesDir))
            {
                var dirName = Path.GetFileName(dir);
                if (dirName.Contains(distroName, StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = Path.Combine(dir, "LocalState", "ext4.vhdx");
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
        }

        return null;
    }

    public async Task<OperationResult<(long Before, long After)>> CompactVhdxAsync(
        string vhdxPath,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default)
    {
        // Step 1: Read size before compact
        var sizeBefore = GetVhdxSize(vhdxPath);
        if (sizeBefore is null)
        {
            return new OperationResult<(long, long)>(false, default,
                $"VHDX file not found: {vhdxPath}");
        }

        // Step 2: Create diskpart script
        progress?.Report(new OperationProgress("Creating diskpart script...", 1, 3));
        _logger.Information("CompactVhdxAsync: creating diskpart script for {VhdxPath}", vhdxPath);

        var scriptPath = Path.Combine(Path.GetTempPath(), $"penguinspace_compact_{Guid.NewGuid():N}.txt");
        var scriptContent =
            $"select vdisk file=\"{vhdxPath}\"\r\n" +
            "attach vdisk readonly\r\n" +
            "compact vdisk\r\n" +
            "detach vdisk\r\n" +
            "exit\r\n";

        await File.WriteAllTextAsync(scriptPath, scriptContent, ct);

        // Step 3: Run diskpart
        progress?.Report(new OperationProgress("Running diskpart compact...", 2, 3));
        _logger.Information("CompactVhdxAsync: running diskpart /s {ScriptPath}", scriptPath);

        CommandResult result;
        try
        {
            result = await _commandRunner.RunAsync("diskpart", $"/s \"{scriptPath}\"", ct);
        }
        finally
        {
            TryDeleteFile(scriptPath);
        }

        if (result.ExitCode != 0)
        {
            _logger.Error(
                "CompactVhdxAsync: diskpart failed (exit {ExitCode}). StdOut: {StdOut} StdErr: {StdErr}",
                result.ExitCode, result.StdOut, result.StdErr);
            return new OperationResult<(long, long)>(false, default,
                $"diskpart failed (exit {result.ExitCode}): {result.StdErr}");
        }

        // Step 4: Read size after compact
        progress?.Report(new OperationProgress("Reading new size...", 3, 3));
        var sizeAfter = GetVhdxSize(vhdxPath) ?? sizeBefore.Value;

        _logger.Information(
            "CompactVhdxAsync: complete. Before={Before} After={After}",
            sizeBefore.Value, sizeAfter);

        return new OperationResult<(long, long)>(true, (sizeBefore.Value, sizeAfter), null);
    }

    public void OpenFolderInExplorer(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            _logger.Warning("OpenFolderInExplorer: folder does not exist: {FolderPath}", folderPath);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folderPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "OpenFolderInExplorer: failed to open {FolderPath}", folderPath);
        }
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to delete temp script file: {Path}", path);
        }
    }
}
