using PenguinSpace.Core.Models;

namespace PenguinSpace.Core.Interfaces;

public interface IDockerService
{
    Task<OperationResult<string>> PruneAsync(CancellationToken ct = default);
}
