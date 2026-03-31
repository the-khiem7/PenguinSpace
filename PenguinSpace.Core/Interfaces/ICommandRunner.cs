using PenguinSpace.Core.Models;

namespace PenguinSpace.Core.Interfaces;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(
        string command,
        string arguments,
        CancellationToken cancellationToken = default,
        int timeoutSeconds = 300
    );
}
