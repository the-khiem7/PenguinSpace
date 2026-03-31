namespace PenguinSpace.Core.Models;

public record OperationProgress(
    string StepDescription,
    int CurrentStep,
    int TotalSteps
);
