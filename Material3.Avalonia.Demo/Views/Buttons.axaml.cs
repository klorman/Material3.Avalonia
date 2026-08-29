using Avalonia.Controls;
using Material3.Avalonia.Demo.ViewModels;

namespace Material3.Avalonia.Demo.Views;

public partial class Buttons : UserControl
{
    public Buttons()
    {
        InitializeComponent();
        DataContext = new ButtonsViewModel();
    }
}
