namespace PenguinSpace.Core.Models;

public record OperationResult<T>(
    bool Success,
    T? Data,
    string? ErrorMessage
);
