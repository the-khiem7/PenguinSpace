using PenguinSpace.Core.Models;

namespace PenguinSpace.Core.Interfaces;

public interface IDistroService
{
    Task<OperationResult<bool>> ResetDistroAsync(
        string distroName,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default
    );
    Task<OperationResult<bool>> RepackDistroAsync(
        string distroName,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default
    );
}
