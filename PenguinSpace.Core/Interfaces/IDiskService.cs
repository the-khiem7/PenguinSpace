using PenguinSpace.Core.Models;

namespace PenguinSpace.Core.Interfaces;

public interface IDiskService
{
    long? GetVhdxSize(string vhdxPath);
    Task<OperationResult<(long Before, long After)>> CompactVhdxAsync(
        string vhdxPath,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default
    );
    void OpenFolderInExplorer(string folderPath);
}
