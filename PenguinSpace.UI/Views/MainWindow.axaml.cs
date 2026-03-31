using Avalonia.Controls;
using PenguinSpace.UI.ViewModels;

namespace PenguinSpace.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
