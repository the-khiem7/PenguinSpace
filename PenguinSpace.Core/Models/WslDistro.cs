namespace PenguinSpace.Core.Models;

public record WslDistro(
    string Name,
    string State,       // "Running" | "Stopped"
    int WslVersion,     // 1 | 2
    string? VhdxPath,
    long? VhdxSizeBytes
);
