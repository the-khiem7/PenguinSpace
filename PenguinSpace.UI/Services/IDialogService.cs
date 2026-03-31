namespace PenguinSpace.UI.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
}
