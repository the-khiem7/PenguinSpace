using PenguinSpace.Core.Models;

namespace PenguinSpace.Core.Interfaces;

public interface IDiskService
{
    string? FindVhdxPath(string distroName);
    long? GetVhdxSize(string vhdxPath);
    Task<OperationResult<(long Before, long After)>> CompactVhdxAsync(
        string vhdxPath,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default
    );
    void OpenFolderInExplorer(string folderPath);
}
