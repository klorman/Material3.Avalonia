using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Material3.Avalonia.Demo.ViewModels;
using Material3.Avalonia.Demo.Views;

namespace Material3.Avalonia.Demo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
            single.MainView = new MainView { DataContext = new MainWindowViewModel() };

        base.OnFrameworkInitializationCompleted();
    }
}