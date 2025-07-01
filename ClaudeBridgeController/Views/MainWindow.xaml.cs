using System.Windows;
using ClaudeBridgeController.ViewModels;

namespace ClaudeBridgeController.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}