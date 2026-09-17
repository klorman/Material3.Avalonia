using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using FluentAssertions;
using Material3.Avalonia.Theme;

namespace Material3.Avalonia.Tests.Controls;

public sealed class HoverStateTests
{
    [Fact]
    public void MouseDeviceSwitchWithinControl_ShouldNotInterruptHover()
    {
        TestApp.EnsureStarted();
        var theme = new MaterialTheme { MotionScheme = null };
        Application.Current!.Styles.Add(theme);
        var button = new Button { Content = "Hover", Width = 200, Height = 100 };
        var window = new Window { Width = 400, Height = 300, Content = button };
        var movementPointer = new Pointer(1, PointerType.Mouse, true);
        var wheelPointer = new Pointer(2, PointerType.Mouse, true);
        try
        {
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var point = button.TranslatePoint(new Point(50, 50), window)!.Value;
            window.MouseMove(point);
            button.Classes.Should().Contain("m3-hovered");
            var drops = 0;
            button.Classes.CollectionChanged += (_, _) =>
            {
                if (!button.Classes.Contains("m3-hovered")) drops++;
            };
            for (var i = 0; i < 5; i++)
            {
                // Replay BrowserInputHandler's device-switch exit/enter pair at the same position.
                button.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent, button, movementPointer,
                    window, point, 0, new PointerPointProperties(), KeyModifiers.None));
                button.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, button, wheelPointer,
                    window, point, 0, new PointerPointProperties(), KeyModifiers.None));
                Dispatcher.UIThread.RunJobs();
            }

            drops.Should().Be(0, "browser wheel and pointermove use different mouse devices at the same location");
            window.MouseMove(new Point(2, 2));
            Dispatcher.UIThread.RunJobs();
            button.Classes.Should().NotContain("m3-hovered");
        }
        finally
        {
            window.Close();
            Application.Current.Styles.Remove(theme);
            Dispatcher.UIThread.RunJobs();
        }
    }
}