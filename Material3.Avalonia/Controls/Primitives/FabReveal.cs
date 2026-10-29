using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class FabReveal : Decorator
{
    private static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<FabReveal, double>("Scale", 1);

    private static readonly StyledProperty<double> AlphaProperty =
        AvaloniaProperty.Register<FabReveal, double>(nameof(Alpha), 1);

    private readonly ScaleTransform _scale = new(1, 1);
    private bool _presented = true;
    private bool _hiddenNotified;

    public double Alpha => GetValue(AlphaProperty);
    public event EventHandler? Hidden;

    public FabReveal()
    {
        RenderTransform = _scale;
        RenderTransformOrigin = RelativePoint.Center;
        Transitions =
        [
            new SpringDoubleTransition
                { Property = ScaleProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast },
            new SpringDoubleTransition
                { Property = AlphaProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast }
        ];
    }

    public void SetPresented(bool presented, bool animate)
    {
        _presented = presented;
        if (presented)
            _hiddenNotified = false;
        var transitions = Transitions;
        if (!animate || MotionSettings.ReduceMotion)
            Transitions = null;
        SetValue(ScaleProperty, presented ? 1 : 0);
        SetValue(AlphaProperty, presented ? 1 : 0);
        Transitions = transitions;
        CompleteExit();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ScaleProperty)
            _scale.ScaleX = _scale.ScaleY = .2 + .8 * Math.Max(0, GetValue(ScaleProperty));
        if (change.Property == AlphaProperty)
        {
            Opacity = Math.Clamp(Alpha, 0, 1);
            CompleteExit();
        }
    }

    private void CompleteExit()
    {
        if (_presented || _hiddenNotified || Alpha != 0)
            return;
        _hiddenNotified = true;
        Hidden?.Invoke(this, EventArgs.Empty);
    }
}