namespace PenguinSpace.Core.Models;

public record CommandResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    TimeSpan Duration
);
