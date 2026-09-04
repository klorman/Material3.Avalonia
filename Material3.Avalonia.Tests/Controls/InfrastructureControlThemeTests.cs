using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using FluentAssertions.Execution;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls.Helpers;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Tests;
using Material3.Avalonia.Tokens;
using MaterialCard = Material3.Avalonia.Controls.Card;
using MaterialCardVariant = Material3.Avalonia.Controls.CardVariant;

namespace Material3.Avalonia.Tests.Controls;

public sealed class InfrastructureControlThemeTests
{
    static InfrastructureControlThemeTests()
    {
        TestApp.EnsureStarted();
    }

    [Fact]
    public void ItemsControlTheme_ShouldForwardContainerProperties()
    {
        var resources = LoadMaterialThemeResources();
        var itemsControl = new ItemsControl
        {
            Theme = GetResource<ControlTheme>(resources, typeof(ItemsControl)),
            Background = Brushes.Red,
            BorderBrush = Brushes.Blue,
            BorderThickness = new Thickness(1, 2, 3, 4),
            CornerRadius = new CornerRadius(5, 6, 7, 8),
            Padding = new Thickness(9, 10, 11, 12)
        };

        itemsControl.ApplyStyling();
        itemsControl.ApplyTemplate();

        var border = itemsControl.GetVisualDescendants()
            .OfType<Border>()
            .Should().ContainSingle()
            .Subject;

        border.Background.Should().BeSameAs(itemsControl.Background);
        border.BorderBrush.Should().BeSameAs(itemsControl.BorderBrush);
        border.BorderThickness.Should().Be(itemsControl.BorderThickness);
        border.CornerRadius.Should().Be(itemsControl.CornerRadius);
        border.Padding.Should().Be(itemsControl.Padding);
    }

    [Fact]
    public void SelectableTextBlockTheme_ShouldProvideContextFlyoutOnlyWhenEnabled()
    {
        var resources = LoadMaterialThemeResources();
        var theme = GetResource<ControlTheme>(resources, typeof(SelectableTextBlock));
        var enabledTextBlock = new SelectableTextBlock
        {
            Theme = theme
        };
        var disabledTextBlock = new SelectableTextBlock
        {
            IsEnabled = false,
            Theme = theme
        };

        enabledTextBlock.ApplyStyling();
        disabledTextBlock.ApplyStyling();

        enabledTextBlock.ContextFlyout.Should().BeOfType<MenuFlyout>();
        disabledTextBlock.ContextFlyout.Should().BeNull();
    }

    [Theory]
    [InlineData(ButtonSize.ExtraSmall, 20d)]
    [InlineData(ButtonSize.Small, 20d)]
    [InlineData(ButtonSize.Medium, 24d)]
    [InlineData(ButtonSize.Large, 32d)]
    [InlineData(ButtonSize.ExtraLarge, 40d)]
    public void ButtonTheme_ShouldApplyLabelLineHeightToContentPresenter(ButtonSize size, double expectedLineHeight)
    {
        var resources = LoadMaterialThemeResources();
        var button = CreateMaterialButton(resources);
        ButtonAssist.SetSize(button, size);

        button.ApplyStyling();
        button.ApplyTemplate();

        var presenter = button.GetVisualDescendants()
            .OfType<ContentPresenter>()
            .Should().ContainSingle(x => x.Name == "PART_ContentPresenter")
            .Subject;
        presenter.ApplyStyling();
        presenter.UpdateChild();

        var textBlock = presenter.Child.Should().BeOfType<TextBlock>().Subject;

        presenter.LineHeight.Should().Be(expectedLineHeight);
        textBlock.LineHeight.Should().Be(expectedLineHeight);
    }

    [Fact]
    public void ButtonTheme_ShouldRespectLocalComponentTokenOverride()
    {
        var resources = LoadMaterialThemeResources();
        var overrideBrush = new SolidColorBrush(Colors.Fuchsia);
        var button = CreateMaterialButton(resources);
        button.Resources["MdCompButtonFilledContainerBrush"] = overrideBrush;
        ButtonAssist.SetVariant(button, ButtonVariant.Filled);

        button.ApplyStyling();

        button.Background.Should().BeSameAs(overrideBrush);
    }

    [Fact]
    public void ButtonTheme_ShouldResolveLocalSystemTokenOverrideThroughComponentAlias()
    {
        var resources = LoadMaterialThemeResources();
        var overrideBrush = new SolidColorBrush(Colors.Red);
        var button = CreateMaterialButton(resources);
        button.Resources["MdSysPrimaryBrush"] = overrideBrush;
        ButtonAssist.SetVariant(button, ButtonVariant.Filled);

        button.ApplyStyling();

        button.Background.Should().BeSameAs(overrideBrush);
    }

    [Fact]
    public void ButtonTheme_ShouldUpdateWhenSystemTokenLeafChanges()
    {
        var resources = LoadMaterialThemeResources();
        var firstBrush = new SolidColorBrush(Colors.Red);
        var secondBrush = new SolidColorBrush(Colors.Blue);
        var button = CreateMaterialButton(resources);
        button.Resources["MdSysPrimaryBrush"] = firstBrush;
        ButtonAssist.SetVariant(button, ButtonVariant.Filled);

        button.ApplyStyling();

        button.Background.Should().BeSameAs(firstBrush);

        button.Resources["MdSysPrimaryBrush"] = secondBrush;

        button.Background.Should().BeSameAs(secondBrush);
    }

    [Fact]
    public void ButtonTheme_ShouldResolveAncestorSystemBrushOverrideThroughComponentAlias()
    {
        var resources = LoadMaterialThemeResources();
        var overrideBrush = new SolidColorBrush(Colors.Orange);
        var (grid, button) = CreateMaterialButtonInGrid(resources);
        grid.Resources["MdSysPrimaryBrush"] = overrideBrush;
        ButtonAssist.SetVariant(button, ButtonVariant.Filled);

        button.ApplyStyling();

        button.Background.Should().BeSameAs(overrideBrush);
    }

    [Fact]
    public void ButtonTheme_ShouldResolveAncestorSystemColorOverrideThroughBrushTokenAlias()
    {
        var resources = LoadMaterialThemeResources();
        var overrideColor = Colors.Orange;
        var (grid, button) = CreateMaterialButtonInGrid(resources);
        grid.Resources["MdSysPrimaryColor"] = overrideColor;
        ButtonAssist.SetVariant(button, ButtonVariant.Filled);

        button.ApplyStyling();

        GetSolidColor(button.Background).Should().Be(overrideColor);
    }

    [Fact]
    public void ButtonTheme_ShouldUpdateWhenAncestorSystemColorLeafChanges()
    {
        var resources = LoadMaterialThemeResources();
        var firstColor = Colors.Orange;
        var secondColor = Colors.Purple;
        var (grid, button) = CreateMaterialButtonInGrid(resources);
        grid.Resources["MdSysPrimaryColor"] = firstColor;
        ButtonAssist.SetVariant(button, ButtonVariant.Filled);

        button.ApplyStyling();

        GetSolidColor(button.Background).Should().Be(firstColor);

        grid.Resources["MdSysPrimaryColor"] = secondColor;

        GetSolidColor(button.Background).Should().Be(secondColor);
    }

    [Fact]
    public void CardTheme_ShouldRespectLocalComponentTokenOverride()
    {
        var resources = LoadMaterialThemeResources();
        var overrideBrush = new SolidColorBrush(Colors.Fuchsia);
        var card = CreateMaterialCard(resources);
        card.Resources["MdCompCardFilledContainerBrush"] = overrideBrush;
        card.Variant = MaterialCardVariant.Filled;

        card.ApplyStyling();

        card.Background.Should().BeSameAs(overrideBrush);
    }

    [Fact]
    public void CardTheme_ShouldResolveLocalSystemTokenOverrideThroughComponentAlias()
    {
        var resources = LoadMaterialThemeResources();
        var overrideBrush = new SolidColorBrush(Colors.Red);
        var card = CreateMaterialCard(resources);
        card.Resources["MdSysSurfaceContainerHighestBrush"] = overrideBrush;
        card.Variant = MaterialCardVariant.Filled;

        card.ApplyStyling();

        card.Background.Should().BeSameAs(overrideBrush);
    }

    [Fact]
    public void CardTheme_ShouldResolveAncestorSystemColorOverrideThroughBrushTokenAlias()
    {
        var resources = LoadMaterialThemeResources();
        var overrideColor = Colors.Orange;
        var (grid, card) = CreateMaterialCardInGrid(resources);
        grid.Resources["MdSysSurfaceContainerHighestColor"] = overrideColor;
        card.Variant = MaterialCardVariant.Filled;

        card.ApplyStyling();

        GetSolidColor(card.Background).Should().Be(overrideColor);
    }

    [Fact]
    public void CardTheme_ShouldResolveLocalSystemSpacingOverrideThroughContentAliases()
    {
        var resources = LoadMaterialThemeResources();
        var card = CreateMaterialCard(resources);
        card.Resources["MdSysSpacing200"] = 3d;

        card.ApplyStyling();

        card.Padding.Should().Be(new Thickness(3));
    }

    [Fact]
    public void TextBoxTheme_ShouldRespectLocalFilledContainerTokenOverride()
    {
        var resources = LoadMaterialThemeResources();
        var overrideBrush = new SolidColorBrush(Colors.Fuchsia);
        var textBox = CreateMaterialTextBox(resources);
        textBox.Resources["MdCompFilledTextFieldContainerBrush"] = overrideBrush;
        TextFieldAssist.SetVariant(textBox, TextFieldVariant.Filled);

        textBox.ApplyStyling();

        textBox.Background.Should().BeSameAs(overrideBrush);
    }

    [Fact]
    public void TextBoxTheme_ShouldResolveLocalSystemTokenOverrideThroughFilledContainerAlias()
    {
        var resources = LoadMaterialThemeResources();
        var overrideBrush = new SolidColorBrush(Colors.Red);
        var textBox = CreateMaterialTextBox(resources);
        textBox.Resources["MdSysSurfaceContainerHighestBrush"] = overrideBrush;
        TextFieldAssist.SetVariant(textBox, TextFieldVariant.Filled);

        textBox.ApplyStyling();

        textBox.Background.Should().BeSameAs(overrideBrush);
    }

    [Fact]
    public void TextBoxTheme_ShouldResolveAncestorSystemColorOverrideThroughFilledContainerBrushAlias()
    {
        var resources = LoadMaterialThemeResources();
        var overrideColor = Colors.Orange;
        var (grid, textBox) = CreateMaterialTextBoxInGrid(resources);
        grid.Resources["MdSysSurfaceContainerHighestColor"] = overrideColor;
        TextFieldAssist.SetVariant(textBox, TextFieldVariant.Filled);

        textBox.ApplyStyling();

        GetSolidColor(textBox.Background).Should().Be(overrideColor);
    }

    [Fact]
    public void TextBoxTheme_ShouldResolveLocalSystemSpacingOverrideThroughFilledLeadingSpaceAlias()
    {
        var resources = LoadMaterialThemeResources();
        var textBox = CreateMaterialTextBox(resources);
        var window = new Window
        {
            Content = textBox
        };
        textBox.Resources["MdSysSpacing200"] = 3d;
        TextFieldAssist.SetVariant(textBox, TextFieldVariant.Filled);

        try
        {
            window.Show();
            textBox.ApplyStyling();
            textBox.ApplyTemplate();

            var leadingSpace = textBox.GetVisualDescendants()
                .OfType<Border>()
                .Should().ContainSingle(x => x.Name == "PART_LeadingSpaceWithoutContent")
                .Subject;

            leadingSpace.IsVisible.Should().BeTrue();
            leadingSpace.Width.Should().Be(3d);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void TextBoxTheme_ShouldResolveLocalRefTypefacePlainOverrideThroughInputFontFamilyAliases()
    {
        var resources = LoadMaterialThemeResources();
        var overrideFontFamily = new FontFamily("Arial");
        var textBox = CreateMaterialTextBox(resources);
        textBox.Resources["MdRefTypefacePlain"] = overrideFontFamily;
        TextFieldAssist.SetVariant(textBox, TextFieldVariant.Filled);

        textBox.ApplyStyling();

        textBox.FontFamily.Should().Be(overrideFontFamily);
    }

    [Fact]
    public void TextBoxTheme_ShouldResolveDefaultRefTypefacePlainThroughInputFontFamilyAliases()
    {
        var resources = LoadMaterialThemeResources();
        var expectedFontFamily = GetResource<FontFamily>(resources, "MdRefTypefacePlain");
        var textBox = new TextBox
        {
            Theme = GetResource<ControlTheme>(resources, typeof(TextBox))
        };
        var window = new Window
        {
            Content = textBox
        };
        window.Resources.MergedDictionaries.Add(resources);
        TextFieldAssist.SetVariant(textBox, TextFieldVariant.Filled);

        try
        {
            window.Show();
            textBox.ApplyStyling();

            textBox.FontFamily.Should().Be(expectedFontFamily);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void TextBoxTheme_ShouldUpdateWhenRefTypefacePlainLeafChangesThroughInputFontFamilyAliases()
    {
        var resources = LoadMaterialThemeResources();
        var firstFontFamily = new FontFamily("Arial");
        var secondFontFamily = new FontFamily("Times New Roman");
        var textBox = CreateMaterialTextBox(resources);
        textBox.Resources["MdRefTypefacePlain"] = firstFontFamily;
        TextFieldAssist.SetVariant(textBox, TextFieldVariant.Filled);

        textBox.ApplyStyling();

        textBox.FontFamily.Should().Be(firstFontFamily);

        textBox.Resources["MdRefTypefacePlain"] = secondFontFamily;

        textBox.FontFamily.Should().Be(secondFontFamily);
    }

    [Fact]
    public void FilledTextBoxTheme_ShouldApplyNormalStateTokensToTemplateParts()
    {
        var resources = LoadMaterialThemeResources();
        var textBox = CreateMaterialStateTextBox(resources, TextFieldVariant.Filled);
        var window = ShowTextFieldWindow(resources, textBox);

        try
        {
            var parts = GetTextFieldParts(textBox);
            DisableTextFieldTransitions(parts);

            parts.FilledIndicator.Thickness.Should()
                .Be(GetResource<double>(resources, "MdCompFilledTextFieldActiveIndicatorHeight"));
            AssertBrushMatchesToken(parts.FilledIndicator.Brush, textBox,
                "MdCompFilledTextFieldActiveIndicatorBrush");
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void FilledTextBoxTheme_ShouldApplyFocusStateTokensToTemplateParts()
    {
        var resources = LoadMaterialThemeResources();
        var textBox = CreateMaterialStateTextBox(resources, TextFieldVariant.Filled);
        var window = ShowTextFieldWindow(resources, textBox);

        try
        {
            var parts = GetTextFieldParts(textBox);
            DisableTextFieldTransitions(parts);
            FocusTextBox(textBox);

            using var scope = new AssertionScope();
            textBox.IsFocused.Should().BeTrue();
            parts.FilledIndicator.Thickness.Should()
                .Be(GetResource<double>(resources, "MdCompFilledTextFieldFocusActiveIndicatorHeight"));
            AssertBrushMatchesToken(parts.FilledIndicator.Brush, textBox,
                "MdCompFilledTextFieldFocusActiveIndicatorBrush");
            AssertBrushMatchesToken(parts.RestingLabel.Foreground, textBox,
                "MdCompFilledTextFieldFocusLabelTextBrush");
            AssertBrushMatchesToken(parts.FilledFloatingLabel.Foreground, textBox,
                "MdCompFilledTextFieldFocusLabelTextBrush");
            AssertBrushMatchesToken(parts.TextPresenter.CaretBrush, textBox,
                "MdCompFilledTextFieldFocusCaretBrush");
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void FilledTextBoxTheme_ShouldApplyManualErrorStateTokensToTemplateParts()
    {
        var resources = LoadMaterialThemeResources();
        var textBox = CreateMaterialStateTextBox(resources, TextFieldVariant.Filled);
        var window = ShowTextFieldWindow(resources, textBox);

        try
        {
            var parts = GetTextFieldParts(textBox);
            DisableTextFieldTransitions(parts);
            TextFieldAssist.SetIsError(textBox, true);
            Dispatcher.UIThread.RunJobs();

            AssertBrushMatchesToken(parts.FilledIndicator.Brush, textBox,
                "MdCompFilledTextFieldErrorActiveIndicatorBrush");
            AssertBrushMatchesToken(parts.RestingLabel.Foreground, textBox,
                "MdCompFilledTextFieldErrorLabelTextBrush");
            AssertBrushMatchesToken(parts.FilledFloatingLabel.Foreground, textBox,
                "MdCompFilledTextFieldErrorLabelTextBrush");
            AssertBrushMatchesToken(parts.SupportingText.Foreground, textBox,
                "MdCompFilledTextFieldErrorSupportingTextBrush");
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void FilledTextBoxTheme_ShouldApplyErrorFocusStateTokensToTemplateParts()
    {
        var resources = LoadMaterialThemeResources();
        var textBox = CreateMaterialStateTextBox(resources, TextFieldVariant.Filled);
        var window = ShowTextFieldWindow(resources, textBox);

        try
        {
            var parts = GetTextFieldParts(textBox);
            DisableTextFieldTransitions(parts);
            TextFieldAssist.SetIsError(textBox, true);
            FocusTextBox(textBox);

            using var scope = new AssertionScope();
            textBox.IsFocused.Should().BeTrue();
            parts.FilledIndicator.Thickness.Should()
                .Be(GetResource<double>(resources, "MdCompFilledTextFieldFocusActiveIndicatorHeight"));
            AssertBrushMatchesToken(parts.FilledIndicator.Brush, textBox,
                "MdCompFilledTextFieldErrorFocusActiveIndicatorBrush");
            AssertBrushMatchesToken(parts.RestingLabel.Foreground, textBox,
                "MdCompFilledTextFieldErrorFocusLabelTextBrush");
            AssertBrushMatchesToken(parts.FilledFloatingLabel.Foreground, textBox,
                "MdCompFilledTextFieldErrorFocusLabelTextBrush");
            AssertBrushMatchesToken(parts.TextPresenter.CaretBrush, textBox,
                "MdCompFilledTextFieldErrorFocusCaretBrush");
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void OutlinedTextBoxTheme_ShouldApplyStateTokensToOutlineTemplatePart()
    {
        var resources = LoadMaterialThemeResources();
        var textBox = CreateMaterialStateTextBox(resources, TextFieldVariant.Outlined);
        var window = ShowTextFieldWindow(resources, textBox);

        try
        {
            var parts = GetTextFieldParts(textBox);
            DisableTextFieldTransitions(parts);
            parts.OutlinedOutline.StrokeThickness.Should()
                .Be(GetResource<double>(resources, "MdCompOutlinedTextFieldOutlineWidth"));
            AssertBrushMatchesToken(parts.OutlinedOutline.Stroke, textBox,
                "MdCompOutlinedTextFieldOutlineBrush");

            FocusTextBox(textBox);
            using (new AssertionScope())
            {
                parts.OutlinedOutline.StrokeThickness.Should()
                    .Be(GetResource<double>(resources, "MdCompOutlinedTextFieldFocusOutlineWidth"));
                AssertBrushMatchesToken(parts.OutlinedOutline.Stroke, textBox,
                    "MdCompOutlinedTextFieldFocusOutlineBrush");
                AssertBrushMatchesToken(parts.RestingLabel.Foreground, textBox,
                    "MdCompOutlinedTextFieldFocusLabelTextBrush");
                AssertBrushMatchesToken(parts.OutlinedFloatingLabel.Foreground, textBox,
                    "MdCompOutlinedTextFieldFocusLabelTextBrush");
                AssertBrushMatchesToken(parts.TextPresenter.CaretBrush, textBox,
                    "MdCompOutlinedTextFieldFocusCaretBrush");
            }

            var nextControl = new Button();
            (window.Content as Panel)!.Children.Add(nextControl);
            nextControl.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();
            textBox.IsFocused.Should().BeFalse();

            TextFieldAssist.SetIsError(textBox, true);
            Dispatcher.UIThread.RunJobs();
            AssertBrushMatchesToken(parts.OutlinedOutline.Stroke, textBox,
                "MdCompOutlinedTextFieldErrorOutlineBrush");
            AssertBrushMatchesToken(parts.RestingLabel.Foreground, textBox,
                "MdCompOutlinedTextFieldErrorLabelTextBrush");
            AssertBrushMatchesToken(parts.OutlinedFloatingLabel.Foreground, textBox,
                "MdCompOutlinedTextFieldErrorLabelTextBrush");
            AssertBrushMatchesToken(parts.SupportingText.Foreground, textBox,
                "MdCompOutlinedTextFieldErrorSupportingTextBrush");

            FocusTextBox(textBox);
            using (new AssertionScope())
            {
                parts.OutlinedOutline.StrokeThickness.Should()
                    .Be(GetResource<double>(resources, "MdCompOutlinedTextFieldFocusOutlineWidth"));
                AssertBrushMatchesToken(parts.OutlinedOutline.Stroke, textBox,
                    "MdCompOutlinedTextFieldErrorFocusOutlineBrush");
                AssertBrushMatchesToken(parts.RestingLabel.Foreground, textBox,
                    "MdCompOutlinedTextFieldErrorFocusLabelTextBrush");
                AssertBrushMatchesToken(parts.OutlinedFloatingLabel.Foreground, textBox,
                    "MdCompOutlinedTextFieldErrorFocusLabelTextBrush");
                AssertBrushMatchesToken(parts.TextPresenter.CaretBrush, textBox,
                    "MdCompOutlinedTextFieldErrorFocusCaretBrush");
            }
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void TextBoxTheme_ShouldNotThrowWhenFilledLabelledFieldLosesFocus()
    {
        var resources = LoadMaterialThemeResources();
        var styles = LoadMaterialThemeStyles();
        var textBox = new TextBox
        {
            Theme = GetResource<ControlTheme>(resources, typeof(TextBox))
        };
        var nextControl = new Button { Content = "Next" };
        var window = new Window
        {
            Width = 400,
            Height = 200,
            Content = new StackPanel
            {
                Children =
                {
                    textBox,
                    nextControl
                }
            }
        };
        window.Resources.MergedDictionaries.Add(resources);
        window.Styles.Add(styles);
        TextFieldAssist.SetVariant(textBox, TextFieldVariant.Filled);
        TextFieldAssist.SetLabel(textBox, "Label");
        textBox.Text = "Selected text";

        var transferFocus = () =>
        {
            window.Show();
            textBox.ApplyStyling();
            textBox.ApplyTemplate();
            textBox.Focus(NavigationMethod.Tab);
            textBox.SelectAll();
            Dispatcher.UIThread.RunJobs();

            nextControl.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();
        };

        try
        {
            transferFocus.Should().NotThrow();
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void TextBoxTheme_ShouldNotThrowWhenDetachedAndReparented()
    {
        var resources = LoadMaterialThemeResources();
        var firstFontFamily = new FontFamily("Arial");
        var secondFontFamily = new FontFamily("Times New Roman");
        var host = new Grid();
        host.Resources.MergedDictionaries.Add(resources);
        host.Resources["MdRefTypefacePlain"] = firstFontFamily;
        var textBox = new TextBox
        {
            Theme = GetResource<ControlTheme>(resources, typeof(TextBox))
        };
        host.Children.Add(textBox);
        TextFieldAssist.SetVariant(textBox, TextFieldVariant.Filled);
        var window = new Window
        {
            Content = host
        };

        try
        {
            window.Show();
            textBox.ApplyStyling();
            textBox.FontFamily.Should().Be(firstFontFamily);

            var detachAndReparent = () =>
            {
                host.Children.Remove(textBox);
                host.Resources["MdRefTypefacePlain"] = secondFontFamily;
                host.Children.Add(textBox);
            };

            detachAndReparent.Should().NotThrow();
            textBox.FontFamily.Should().Be(secondFontFamily);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void ButtonTheme_ShouldUpdateWhenRefTypefaceWeightMediumLeafChangesThroughLabelWeightAliases()
    {
        var resources = LoadMaterialThemeResources();
        var button = CreateMaterialButton(resources);
        button.Resources["MdRefTypefaceWeightMedium"] = FontWeight.Bold;
        ButtonAssist.SetSize(button, ButtonSize.Small);

        button.ApplyStyling();

        button.FontWeight.Should().Be(FontWeight.Bold);

        button.Resources["MdRefTypefaceWeightMedium"] = FontWeight.Black;

        button.FontWeight.Should().Be(FontWeight.Black);
    }

    [Fact]
    public void TypographyStyle_ShouldResolveDefaultRefTypefacePlainThroughSystemToken()
    {
        var resources = LoadMaterialThemeResources();
        var styles = LoadMaterialThemeStyles();
        var expectedFontFamily = GetResource<FontFamily>(resources, "MdRefTypefacePlain");
        var control = new TextBlock { Text = "Text" };
        Typography.SetTextStyle(control, MdTextStyle.BodyLarge);

        var window = ShowTypographyWindow(resources, styles, control);
        try
        {
            control.FontFamily.Should().Be(expectedFontFamily);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void TypographyStyle_ShouldResolveLocalRefTypefacePlainOverrideThroughSystemToken()
    {
        var resources = LoadMaterialThemeResources();
        var styles = LoadMaterialThemeStyles();
        var overrideFontFamily = new FontFamily("Arial");
        var control = new TextBlock { Text = "Text" };
        Typography.SetTextStyle(control, MdTextStyle.BodyLarge);

        var window = ShowTypographyWindow(resources, styles, control);
        try
        {
            window.Resources["MdRefTypefacePlain"] = overrideFontFamily;

            control.FontFamily.Should().Be(overrideFontFamily);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void TypographyStyle_ShouldResolveLocalSystemFontFamilyOverride()
    {
        var resources = LoadMaterialThemeResources();
        var styles = LoadMaterialThemeStyles();
        var overrideFontFamily = new FontFamily("Arial");
        var control = new TextBlock { Text = "Text" };
        Typography.SetTextStyle(control, MdTextStyle.BodyLarge);

        var window = ShowTypographyWindow(resources, styles, control);
        try
        {
            window.Resources["MdSysTypeScaleBodyLargeFontFamily"] = overrideFontFamily;

            control.FontFamily.Should().Be(overrideFontFamily);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void TypographyStyle_ShouldUpdateWhenRefTypefacePlainLeafChangesThroughSystemToken()
    {
        var resources = LoadMaterialThemeResources();
        var styles = LoadMaterialThemeStyles();
        var firstFontFamily = new FontFamily("Arial");
        var secondFontFamily = new FontFamily("Times New Roman");
        var control = new TextBlock { Text = "Text" };
        Typography.SetTextStyle(control, MdTextStyle.BodyLarge);

        var window = ShowTypographyWindow(resources, styles, control);
        try
        {
            window.Resources["MdRefTypefacePlain"] = firstFontFamily;

            control.FontFamily.Should().Be(firstFontFamily);

            window.Resources["MdRefTypefacePlain"] = secondFontFamily;

            control.FontFamily.Should().Be(secondFontFamily);
        }
        finally
        {
            window.Close();
        }
    }

    private static IResourceDictionary LoadMaterialThemeResources()
    {
        return AvaloniaXamlLoader.Load(new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml"))
            .Should().BeAssignableTo<IResourceDictionary>()
            .Subject;
    }

    private static Styles LoadMaterialThemeStyles()
    {
        return AvaloniaXamlLoader.Load(new Uri("avares://Material3.Avalonia/Theme/MaterialThemeStyles.axaml"))
            .Should().BeAssignableTo<Styles>()
            .Subject;
    }

    private static Window ShowTypographyWindow(IResourceDictionary resources, Styles styles, Control control)
    {
        var window = new Window
        {
            Content = control
        };
        window.Resources.MergedDictionaries.Add(resources);
        window.Styles.Add(styles);
        window.Show();
        control.ApplyStyling();

        return window;
    }

    private static Button CreateMaterialButton(IResourceDictionary resources)
    {
        var button = new Button
        {
            Theme = GetResource<ControlTheme>(resources, typeof(Button)),
            Content = "Label"
        };
        button.Resources.MergedDictionaries.Add(resources);

        return button;
    }

    private static (Grid Grid, Button Button) CreateMaterialButtonInGrid(IResourceDictionary resources)
    {
        var grid = new Grid();
        grid.Resources.MergedDictionaries.Add(resources);

        var button = new Button
        {
            Theme = GetResource<ControlTheme>(resources, typeof(Button)),
            Content = "Label"
        };
        grid.Children.Add(button);

        return (grid, button);
    }

    private static MaterialCard CreateMaterialCard(IResourceDictionary resources)
    {
        var card = new MaterialCard
        {
            Theme = GetResource<ControlTheme>(resources, typeof(MaterialCard)),
            Content = "Content"
        };
        card.Resources.MergedDictionaries.Add(resources);

        return card;
    }

    private static (Grid Grid, MaterialCard Card) CreateMaterialCardInGrid(IResourceDictionary resources)
    {
        var grid = new Grid();
        grid.Resources.MergedDictionaries.Add(resources);

        var card = new MaterialCard
        {
            Theme = GetResource<ControlTheme>(resources, typeof(MaterialCard)),
            Content = "Content"
        };
        grid.Children.Add(card);

        return (grid, card);
    }

    private static TextBox CreateMaterialTextBox(IResourceDictionary resources)
    {
        var textBox = new TextBox
        {
            Theme = GetResource<ControlTheme>(resources, typeof(TextBox))
        };
        textBox.Resources.MergedDictionaries.Add(resources);

        return textBox;
    }

    private static (Grid Grid, TextBox TextBox) CreateMaterialTextBoxInGrid(IResourceDictionary resources)
    {
        var grid = new Grid();
        grid.Resources.MergedDictionaries.Add(resources);

        var textBox = new TextBox
        {
            Theme = GetResource<ControlTheme>(resources, typeof(TextBox))
        };
        grid.Children.Add(textBox);

        return (grid, textBox);
    }

    private static TextBox CreateMaterialStateTextBox(IResourceDictionary resources, TextFieldVariant variant)
    {
        var textBox = new TextBox
        {
            Theme = GetResource<ControlTheme>(resources, typeof(TextBox)),
            Text = "Input",
            Width = 300
        };
        TextFieldAssist.SetVariant(textBox, variant);
        TextFieldAssist.SetLabel(textBox, "Label");
        TextFieldAssist.SetSupportingText(textBox, "Supporting");
        TextFieldAssist.SetErrorText(textBox, "Error");
        AddTextFieldStateBrushOverrides(textBox);

        return textBox;
    }

    private static void AddTextFieldStateBrushOverrides(TextBox textBox)
    {
        textBox.Resources["MdCompFilledTextFieldActiveIndicatorBrush"] = TestBrush(1);
        textBox.Resources["MdCompFilledTextFieldFocusActiveIndicatorBrush"] = TestBrush(2);
        textBox.Resources["MdCompFilledTextFieldLabelTextBrush"] = TestBrush(3);
        textBox.Resources["MdCompFilledTextFieldFocusLabelTextBrush"] = TestBrush(4);
        textBox.Resources["MdCompFilledTextFieldCaretBrush"] = TestBrush(5);
        textBox.Resources["MdCompFilledTextFieldFocusCaretBrush"] = TestBrush(6);
        textBox.Resources["MdCompFilledTextFieldErrorActiveIndicatorBrush"] = TestBrush(7);
        textBox.Resources["MdCompFilledTextFieldErrorLabelTextBrush"] = TestBrush(8);
        textBox.Resources["MdCompFilledTextFieldErrorSupportingTextBrush"] = TestBrush(9);
        textBox.Resources["MdCompFilledTextFieldErrorFocusActiveIndicatorBrush"] = TestBrush(10);
        textBox.Resources["MdCompFilledTextFieldErrorFocusLabelTextBrush"] = TestBrush(11);
        textBox.Resources["MdCompFilledTextFieldErrorFocusCaretBrush"] = TestBrush(12);

        textBox.Resources["MdCompOutlinedTextFieldOutlineBrush"] = TestBrush(20);
        textBox.Resources["MdCompOutlinedTextFieldFocusOutlineBrush"] = TestBrush(21);
        textBox.Resources["MdCompOutlinedTextFieldLabelTextBrush"] = TestBrush(22);
        textBox.Resources["MdCompOutlinedTextFieldFocusLabelTextBrush"] = TestBrush(23);
        textBox.Resources["MdCompOutlinedTextFieldCaretBrush"] = TestBrush(24);
        textBox.Resources["MdCompOutlinedTextFieldFocusCaretBrush"] = TestBrush(25);
        textBox.Resources["MdCompOutlinedTextFieldErrorOutlineBrush"] = TestBrush(26);
        textBox.Resources["MdCompOutlinedTextFieldErrorLabelTextBrush"] = TestBrush(27);
        textBox.Resources["MdCompOutlinedTextFieldErrorSupportingTextBrush"] = TestBrush(28);
        textBox.Resources["MdCompOutlinedTextFieldErrorFocusOutlineBrush"] = TestBrush(29);
        textBox.Resources["MdCompOutlinedTextFieldErrorFocusLabelTextBrush"] = TestBrush(30);
        textBox.Resources["MdCompOutlinedTextFieldErrorFocusCaretBrush"] = TestBrush(31);
    }

    private static SolidColorBrush TestBrush(byte value)
    {
        return new SolidColorBrush(Color.FromRgb(value, (byte)(value + 1), (byte)(value + 2)));
    }

    private static Window ShowTextFieldWindow(IResourceDictionary resources, TextBox textBox)
    {
        var window = new Window
        {
            Width = 400,
            Height = 200,
            Content = new StackPanel
            {
                Children =
                {
                    textBox
                }
            }
        };
        window.Resources.MergedDictionaries.Add(resources);
        window.Show();
        textBox.ApplyStyling();
        textBox.ApplyTemplate();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    private static void FocusTextBox(TextBox textBox)
    {
        textBox.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
    }

    private static void DisableTextFieldTransitions(TextFieldParts parts)
    {
        parts.FilledIndicator.Transitions = null;
        parts.OutlinedOutline.Transitions = null;
        parts.TextPresenter.Transitions = null;
        parts.RestingLabel.Transitions = null;
        parts.FilledFloatingLabel.Transitions = null;
        parts.OutlinedFloatingLabel.Transitions = null;
        parts.SupportingText.Transitions = null;
    }

    private static TextFieldParts GetTextFieldParts(TextBox textBox)
    {
        return new TextFieldParts(
            GetTemplatePart<TextFieldIndicatorLine>(textBox, "PART_FilledIndicator"),
            GetTemplatePart<NotchedOutline>(textBox, "PART_OutlinedOutline"),
            GetTemplatePart<TextPresenter>(textBox, "PART_TextPresenter"),
            GetTemplatePart<TextBlock>(textBox, "PART_RestingLabel"),
            GetTemplatePart<TextBlock>(textBox, "PART_FilledFloatingLabel"),
            GetTemplatePart<TextBlock>(textBox, "PART_OutlinedFloatingLabel"),
            GetTemplatePart<TextBlock>(textBox, "PART_SupportingText"));
    }

    private static TPart GetTemplatePart<TPart>(TextBox textBox, string name)
        where TPart : Control
    {
        return textBox.GetVisualDescendants()
            .OfType<TPart>()
            .Should().ContainSingle(x => x.Name == name)
            .Subject;
    }

    private static void AssertBrushMatchesToken(IBrush? brush, IResourceHost resourceHost, object tokenKey)
    {
        var expectedBrush = ResolveToken<IBrush>(resourceHost, tokenKey);

        GetSolidColor(brush).Should().Be(GetSolidColor(expectedBrush));
    }

    private static TResource ResolveToken<TResource>(IResourceHost resourceHost, object tokenKey)
    {
        var visited = new HashSet<object>();
        var currentKey = tokenKey;

        while (true)
        {
            visited.Add(currentKey).Should().BeTrue($"token alias cycle should not exist at {currentKey}");
            resourceHost.TryFindResource(currentKey, GetThemeVariant(resourceHost), out var resource)
                .Should().BeTrue($"token resource {currentKey} should exist");

            if (resource is not TokenAlias alias)
                return resource.Should().BeAssignableTo<TResource>().Subject;

            alias.ResourceKey.Should().NotBeNull($"token alias {currentKey} should point to another key");
            currentKey = alias.ResourceKey;
        }
    }

    private static ThemeVariant? GetThemeVariant(IResourceHost resourceHost)
    {
        return resourceHost is StyledElement element ? element.ActualThemeVariant : null;
    }

    private static Color GetSolidColor(IBrush? brush)
    {
        return brush.Should().BeAssignableTo<ISolidColorBrush>().Subject.Color;
    }

    private sealed record TextFieldParts(
        TextFieldIndicatorLine FilledIndicator,
        NotchedOutline OutlinedOutline,
        TextPresenter TextPresenter,
        TextBlock RestingLabel,
        TextBlock FilledFloatingLabel,
        TextBlock OutlinedFloatingLabel,
        TextBlock SupportingText);

    private static TResource GetResource<TResource>(IResourceDictionary resources, object key)
    {
        resources.TryGetResource(key, null, out var resource)
            .Should().BeTrue();

        return resource.Should().BeOfType<TResource>().Subject;
    }
}
