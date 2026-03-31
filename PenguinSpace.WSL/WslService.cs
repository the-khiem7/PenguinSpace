using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using Serilog;

namespace PenguinSpace.WSL;

public class WslService : IWslService
{
    private readonly ICommandRunner _commandRunner;
    private readonly ILogger _logger;

    public WslService(ICommandRunner commandRunner, ILogger logger)
    {
        _commandRunner = commandRunner;
        _logger = logger;
    }

    public async Task<OperationResult<IReadOnlyList<WslDistro>>> ListDistrosAsync(CancellationToken ct = default)
    {
        _logger.Information("Listing WSL distros via 'wsl -l -v'");

        try
        {
            var result = await _commandRunner.RunAsync("wsl", "-l -v", ct);

            if (result.ExitCode != 0)
            {
                var error = string.IsNullOrWhiteSpace(result.StdErr)
                    ? $"wsl -l -v failed with exit code {result.ExitCode}"
                    : result.StdErr.Trim();

                _logger.Error("wsl -l -v failed: ExitCode={ExitCode}, StdErr={StdErr}", result.ExitCode, error);
                return new OperationResult<IReadOnlyList<WslDistro>>(false, null, error);
            }

            var distros = ParseWslOutput(result.StdOut);
            _logger.Information("Found {Count} WSL distro(s)", distros.Count);
            return new OperationResult<IReadOnlyList<WslDistro>>(true, distros, null);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while listing WSL distros");
            return new OperationResult<IReadOnlyList<WslDistro>>(false, null, ex.Message);
        }
    }

    public async Task<OperationResult<bool>> ShutdownAsync(CancellationToken ct = default)
    {
        _logger.Information("Shutting down WSL via 'wsl --shutdown'");

        try
        {
            var result = await _commandRunner.RunAsync("wsl", "--shutdown", ct);

            if (result.ExitCode != 0)
            {
                var error = string.IsNullOrWhiteSpace(result.StdErr)
                    ? $"wsl --shutdown failed with exit code {result.ExitCode}"
                    : result.StdErr.Trim();

                _logger.Error("wsl --shutdown failed: ExitCode={ExitCode}, StdErr={StdErr}", result.ExitCode, error);
                return new OperationResult<bool>(false, false, error);
            }

            _logger.Information("WSL shutdown completed successfully");
            return new OperationResult<bool>(true, true, null);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while shutting down WSL");
            return new OperationResult<bool>(false, false, ex.Message);
        }
    }

    internal static List<WslDistro> ParseWslOutput(string output)
    {
        var distros = new List<WslDistro>();

        if (string.IsNullOrWhiteSpace(output))
            return distros;

        // WSL outputs UTF-16 which may contain null characters when read as a string
        var cleaned = output.Replace("\0", "");

        var lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip empty lines and header row
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.Contains("NAME", StringComparison.OrdinalIgnoreCase))
                continue;

            // Strip leading '*' which marks the default distro
            var dataLine = trimmed.TrimStart('*').Trim();

            var parts = dataLine.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
                continue;

            var name = parts[0];
            var state = parts[1];

            if (!int.TryParse(parts[2], out var version))
                continue;

            distros.Add(new WslDistro(name, state, version, null, null));
        }

        return distros;
    }
}
