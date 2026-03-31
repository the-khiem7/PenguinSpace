using PenguinSpace.Core.Models;

namespace PenguinSpace.Core.Interfaces;

public interface IWslService
{
    Task<OperationResult<IReadOnlyList<WslDistro>>> ListDistrosAsync(CancellationToken ct = default);
    Task<OperationResult<bool>> ShutdownAsync(CancellationToken ct = default);
}
