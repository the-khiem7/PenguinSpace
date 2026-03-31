using PenguinSpace.Core.Interfaces;
using PenguinSpace.Core.Models;
using Serilog;

namespace PenguinSpace.Docker;

public class DockerService : IDockerService
{
    private readonly ICommandRunner _commandRunner;
    private readonly ILogger _logger;

    public DockerService(ICommandRunner commandRunner, ILogger logger)
    {
        _commandRunner = commandRunner;
        _logger = logger;
    }

    public async Task<OperationResult<string>> PruneAsync(CancellationToken ct = default)
    {
        _logger.Information("Starting Docker cleanup via 'docker system prune -a --volumes -f'");

        try
        {
            var result = await _commandRunner.RunAsync("docker", "system prune -a --volumes -f", ct);

            if (result.ExitCode != 0)
            {
                var stderr = result.StdErr.Trim();

                if (stderr.Contains("daemon", StringComparison.OrdinalIgnoreCase)
                    || stderr.Contains("not running", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Error("Docker daemon is not running");
                    return new OperationResult<string>(false, null, "Docker daemon không hoạt động");
                }

                var error = string.IsNullOrWhiteSpace(stderr)
                    ? $"docker system prune failed with exit code {result.ExitCode}"
                    : stderr;

                _logger.Error("Docker prune failed: ExitCode={ExitCode}, StdErr={StdErr}", result.ExitCode, error);
                return new OperationResult<string>(false, null, error);
            }

            var reclaimedSpace = ParseReclaimedSpace(result.StdOut);
            _logger.Information("Docker cleanup completed. Reclaimed: {ReclaimedSpace}", reclaimedSpace);
            return new OperationResult<string>(true, reclaimedSpace, null);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception during Docker cleanup");
            return new OperationResult<string>(false, null, ex.Message);
        }
    }

    internal static string ParseReclaimedSpace(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return "0B";

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Total reclaimed space:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed["Total reclaimed space:".Length..].Trim();
            }
        }

        return "0B";
    }
}
