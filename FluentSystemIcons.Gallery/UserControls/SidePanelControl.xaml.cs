using FluentSystemIcons;
using FluentSystemIcons.Gallery.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Numerics;
using System.Text;
using Windows.ApplicationModel.DataTransfer;

namespace FluentSystemIcons.Gallery.UserControls
{
    public partial class SidePanelControl : UserControl
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(IconInfo), typeof(SidePanelControl), new PropertyMetadata(null, OnSelectedItemChanged));
        public IconInfo? SelectedItem
        {
            get => GetValue(SelectedItemProperty) as IconInfo;
            set => SetValue(SelectedItemProperty, value);
        }
        public Func<IconInfo, bool>? IsFavoriteChecker { get; set; }
        public event Action<IconInfo, bool>? FavoriteToggled;

        public Window? ParentWindow { get; set; }

        private readonly DispatcherTimer _copyFeedbackTimer;
        private ToolTip? _activeToolTip;
        private (FontIcon CopyIcon, FontIcon CheckIcon)? _activeIcons;

        private object? _originalToolTipObj;
        private Button? _activeToolTipButton;

        public SidePanelControl()
        {
            this.InitializeComponent();
            UpdateUI();
            UpdateCodeViewUI(CodeViewMode.Xaml);

            _copyFeedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _copyFeedbackTimer.Tick += OnCopyFeedbackTimerTick;
            this.Unloaded += (s, e) => _copyFeedbackTimer.Stop();
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SidePanelControl control)
            {
                control.UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (SelectedItem != null)
            {
                IconDisplayTextBlock.FontFamily = SelectedItem.FontFamily;
                IconDisplayTextBlock.Glyph = SelectedItem.Glyph;
                IconNameValueTextBlock.Text = SelectedItem.DisplayName;

                PreviewButtonIcon.FontFamily = SelectedItem.FontFamily;
                PreviewButtonIcon.Glyph = SelectedItem.Glyph;

                PreviewCircularIcon.FontFamily = SelectedItem.FontFamily;
                PreviewCircularIcon.Glyph = SelectedItem.Glyph;

                PreviewCircularIconSecondary.FontFamily = SelectedItem.FontFamily;
                PreviewCircularIconSecondary.Glyph = SelectedItem.Glyph;

                PreviewNavIcon.FontFamily = SelectedItem.FontFamily;
                PreviewNavIcon.Glyph = SelectedItem.Glyph;

                bool isFav = IsFavoriteChecker?.Invoke(SelectedItem) ?? false;
                UpdateFavoriteButtonState(isFav);

                SvgIconValueTextBlock.Text = GenerateSvgMarkup(SelectedItem);

                GlyphCodeValueTextBlock.Text = SelectedItem.XamlCode;
                FontIconValueTextBlock.Text = $"<FontIcon FontFamily=\"{{StaticResource {SelectedItem.FontKey}}}\" Glyph=\"{SelectedItem.XamlCode}\" />";

                if (!string.IsNullOrEmpty(SelectedItem.Glyph))
                {
                    int codepoint = SelectedItem.Glyph[0];
                    string hexCode = codepoint.ToString("X4");
                    string literalGlyphCode = $"\\u{hexCode}";

                    CSharpGlyphCodeValueTextBlock.Text = literalGlyphCode;
                    CSharpFontIconValueTextBlock.Text = $"var fontIcon = new FontIcon {{ Glyph = \"{literalGlyphCode}\" }};";
                }
                else
                {
                    CSharpGlyphCodeValueTextBlock.Text = string.Empty;
                    CSharpFontIconValueTextBlock.Text = string.Empty;
                }

                InitialMessageTextBlock.Visibility = Visibility.Collapsed;
                AnimateOpacity(DetailsGrid, 1.0);
            }
            else
            {
                InitialMessageTextBlock.Visibility = Visibility.Visible;
                AnimateOpacity(DetailsGrid, 0.0);
            }
        }

        public string GenerateSvgMarkup(IconInfo icon)
        {
            try
            {
                var device = CanvasDevice.GetSharedDevice();

                float renderSize = 1024.0f;
                using (var textFormat = new Microsoft.Graphics.Canvas.Text.CanvasTextFormat
                {
                    FontFamily = icon.FontFamily.Source,
                    FontSize = renderSize
                })
                using (var textLayout = new Microsoft.Graphics.Canvas.Text.CanvasTextLayout(device, icon.Glyph, textFormat, renderSize * 2, renderSize * 2))
                {
                    using (var rawGeometry = CanvasGeometry.CreateText(textLayout))
                    {
                        var bounds = rawGeometry.ComputeBounds();
                        if (bounds.Width <= 0 || bounds.Height <= 0)
                        {
                            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\">\n  <text font-family=\"{icon.FontKey}\" font-size=\"24\" fill=\"currentColor\">{icon.XamlCode}</text>\n</svg>";
                        }

                        float targetActiveArea = 20.0f;
                        float maxDimension = (float)Math.Max(bounds.Width, bounds.Height);
                        float scale = targetActiveArea / maxDimension;

                        var unscaledCenter = new Vector2((float)(bounds.X + bounds.Width / 2.0), (float)(bounds.Y + bounds.Height / 2.0));
                        var translation = new Vector2(12.0f, 12.0f) - (unscaledCenter * scale);
                        var transformMatrix = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(translation);

                        using (var normalizedGeometry = rawGeometry.Transform(transformMatrix))
                        {
                            var pathReceiver = new SvgPathReceiver();

                            normalizedGeometry.SendPathTo(pathReceiver);

                            string pathData = pathReceiver.GetPathData();

                            if (string.IsNullOrWhiteSpace(pathData))
                            {
                                return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\">\n  <text font-family=\"{icon.FontKey}\" font-size=\"24\" fill=\"currentColor\">{icon.XamlCode}</text>\n</svg>";
                            }

                            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" width=\"24\" height=\"24\">\n  <path d=\"{pathData.Trim()}\" fill=\"currentColor\" fill-rule=\"evenodd\" />\n</svg>";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\">\n  <!-- Error de renderizado vectorial: {ex.Message} -->\n  <text font-family=\"{icon.FontKey}\" font-size=\"24\" fill=\"currentColor\">{icon.XamlCode}</text>\n</svg>";
            }
        }

        private void UpdateCodeViewUI(CodeViewMode mode)
        {
            bool isXaml = (mode == CodeViewMode.Xaml);
            var xamlVisibility = isXaml ? Visibility.Visible : Visibility.Collapsed;
            var csharpVisibility = isXaml ? Visibility.Collapsed : Visibility.Visible;

            var resourceManager = new ResourceManager();
            GlyphCodeTitleTextBlock.Text = isXaml ? resourceManager.MainResourceMap.GetValue("Resources/SidePanel_GlyphTextHeader_Text").ValueAsString : resourceManager.MainResourceMap.GetValue("Resources/SidePanel_GlyphCodeHeader_Text").ValueAsString;
            FontIconTitleTextBlock.Text = isXaml ? resourceManager.MainResourceMap.GetValue("Resources/SidePanel_FontIconXamlHeader_Text").ValueAsString : resourceManager.MainResourceMap.GetValue("Resources/SidePanel_FontIconCSharpHeader_Text").ValueAsString;

            GlyphCodeValueTextBlock.Visibility = xamlVisibility;
            CopyGlyphButton.Visibility = xamlVisibility;
            CSharpGlyphCodeValueTextBlock.Visibility = csharpVisibility;
            CopyCSharpGlyphButton.Visibility = csharpVisibility;

            FontIconValueTextBlock.Visibility = xamlVisibility;
            CopyFontIconButton.Visibility = xamlVisibility;
            CSharpFontIconValueTextBlock.Visibility = csharpVisibility;
            CopyCSharpFontIconButton.Visibility = csharpVisibility;
        }

        private void CodeSwitcher_SelectionChanged(CodeViewSwitcher sender, CodeViewMode args)
        {
            UpdateCodeViewUI(args);
        }

        #region Botones de Copiado y Animación
        private void CopyToClipboardAndShowConfirmation(string text, object? toolTipObj, Button button)
        {
            if (string.IsNullOrEmpty(text) || button == null) return;
            if (_copyFeedbackTimer.IsEnabled) OnCopyFeedbackTimerTick(null, null);
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
            var copyIcon = FindVisualChildByTag<FontIcon>(button, "Copy");
            var checkIcon = FindVisualChildByTag<FontIcon>(button, "Check");
            if (copyIcon != null && checkIcon != null)
            {
                _activeIcons = (copyIcon, checkIcon);
                _originalToolTipObj = toolTipObj;
                _activeToolTipButton = button;
                AnimateCopyIcon(copyIcon, checkIcon);
                var resourceManager = new ResourceManager();
                string copiedText = resourceManager.MainResourceMap.GetValue("Resources/SidePanel_Copied_Text").ValueAsString;
                var tempToolTip = new ToolTip
                {
                    Content = copiedText,
                    Placement = PlacementMode.Top
                };
                ToolTipService.SetToolTip(button, tempToolTip);
                _activeToolTip = tempToolTip;
                _activeToolTip.IsOpen = true;
                _copyFeedbackTimer.Start();
            }
        }

        private void AnimateCopyIcon(UIElement copyIcon, UIElement checkIcon, bool reverse = false)
        {
            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(150);
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            var fadeOut = new DoubleAnimation { To = reverse ? 1.0 : 0.0, Duration = duration, EasingFunction = easing };
            Storyboard.SetTarget(fadeOut, copyIcon);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");
            var fadeIn = new DoubleAnimation { To = reverse ? 0.0 : 1.0, Duration = duration, EasingFunction = easing };
            Storyboard.SetTarget(fadeIn, checkIcon);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            storyboard.Children.Add(fadeOut);
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();
        }

        private void OnCopyFeedbackTimerTick(object? sender, object? e)
        {
            _copyFeedbackTimer.Stop();
            if (_activeIcons.HasValue)
            {
                AnimateCopyIcon(_activeIcons.Value.CopyIcon, _activeIcons.Value.CheckIcon, reverse: true);
                _activeIcons = null;
            }
            if (_activeToolTip != null)
            {
                _activeToolTip.IsOpen = false;
                _activeToolTip = null;
            }
            if (_activeToolTipButton != null)
            {
                ToolTipService.SetToolTip(_activeToolTipButton, _originalToolTipObj);
                _activeToolTipButton = null;
                _originalToolTipObj = null;
            }
        }

        private void CopyButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var toolTipObj = ToolTipService.GetToolTip(button);
            if (toolTipObj is null || _activeToolTip == toolTipObj as ToolTip) return;

            var resourceManager = new ResourceManager();
            string resourceKey = string.Empty;
            switch (button.Name)
            {
                case "CopyNameButton": resourceKey = "CopyNameButton.ToolTipService.ToolTip"; break;
                case "CopyGlyphButton": resourceKey = "CopyGlyphButton.ToolTipService.ToolTip"; break;
                case "CopyCSharpGlyphButton": resourceKey = "CopyCSharpGlyphButton.ToolTipService.ToolTip"; break;
                case "CopyFontIconButton": resourceKey = "CopyFontIconButton.ToolTipService.ToolTip"; break;
                case "CopyCSharpFontIconButton": resourceKey = "CopyCSharpFontIconButton.ToolTipService.ToolTip"; break;
            }
            if (!string.IsNullOrEmpty(resourceKey))
            {
                var resource = resourceManager.MainResourceMap.TryGetValue($"Resources/{resourceKey}");
                if (resource != null)
                {
                    ToolTipService.SetToolTip(button, resource.ValueAsString);
                }
                else if (button.Name == "CopySvgIconButton")
                {
                    ToolTipService.SetToolTip(button, resourceManager.MainResourceMap.GetValue("Resources/SidePanel_Copy_Svg_Tooltip").ValueAsString);
                }
            }
        }

        private void HandleCopyClick(object sender, Func<IconInfo, string> getTextFunc)
        {
            if (sender is Button button && SelectedItem != null)
            {
                var toolTipObj = ToolTipService.GetToolTip(button);
                var textToCopy = getTextFunc(SelectedItem);
                CopyToClipboardAndShowConfirmation(textToCopy, toolTipObj, button);
            }
        }

        private void CopyNameButton_Click(object sender, RoutedEventArgs e) => HandleCopyClick(sender, item => item.DisplayName);
        private void CopyGlyphButton_Click(object sender, RoutedEventArgs e) => HandleCopyClick(sender, item => item.XamlCode);
        private void CopyFontIconButton_Click(object sender, RoutedEventArgs e) => HandleCopyClick(sender, item => $"<FontIcon FontFamily=\"{{StaticResource {item.FontKey}}}\" Glyph=\"{item.XamlCode}\" />");
        private void CopyCSharpGlyphButton_Click(object sender, RoutedEventArgs e) => HandleCopyClick(sender, item => CSharpGlyphCodeValueTextBlock.Text);
        private void CopyCSharpFontIconButton_Click(object sender, RoutedEventArgs e) => HandleCopyClick(sender, item => CSharpFontIconValueTextBlock.Text);

        private void CopySvgIconButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && SelectedItem != null)
            {
                var toolTipObj = ToolTipService.GetToolTip(button);
                var textToCopy = SvgIconValueTextBlock.Text;
                CopyToClipboardAndShowConfirmation(textToCopy, toolTipObj, button);
            }
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null) return;

            bool isFav = IsFavoriteChecker?.Invoke(SelectedItem) ?? false;
            bool newFavState = !isFav;
            FavoriteToggled?.Invoke(SelectedItem, newFavState);
            UpdateFavoriteButtonState(newFavState);
        }

        private void UpdateFavoriteButtonState(bool isFav)
        {
            var resourceManager = new ResourceManager();
            if (isFav)
            {
                FavoriteButtonIcon.FontFamily = App.Current.Resources["FluentIconsFilled"] as FontFamily;
                FavoriteButtonIcon.Glyph = FluentSystemIconsFilled.Star20;
                FavoriteButtonIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
                FavoriteButtonToolTip.Content = resourceManager.MainResourceMap.GetValue("Resources/SidePanel_Favorite_Remove").ValueAsString;
            }
            else
            {
                FavoriteButtonIcon.FontFamily = App.Current.Resources["FluentIconsRegular"] as FontFamily;
                FavoriteButtonIcon.Glyph = FluentSystemIconsRegular.Star20;
                FavoriteButtonIcon.Foreground = App.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
                FavoriteButtonToolTip.Content = resourceManager.MainResourceMap.GetValue("Resources/SidePanel_Favorite_Add").ValueAsString;
            }
        }

        private async System.Threading.Tasks.Task ExportSvgAsFileAsync(IconInfo icon)
        {
            try
            {
                if (ParentWindow == null) return;
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(ParentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
                savePicker.FileTypeChoices.Add("Scalable Vector Graphics", new System.Collections.Generic.List<string>() { ".svg" });
                savePicker.SuggestedFileName = $"{icon.Name}.svg";
                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    string svgContent = GenerateSvgMarkup(icon);
                    await Windows.Storage.FileIO.WriteTextAsync(file, svgContent);
                }
            }
            catch (Exception ex)
            {
                var resourceManager = new ResourceManager();
                var dialog = new ContentDialog
                {
                    Title = resourceManager.MainResourceMap.GetValue("Resources/Export_Error_Title").ValueAsString,
                    Content = string.Format(resourceManager.MainResourceMap.GetValue("Resources/Export_Error_Message").ValueAsString, ex.Message),
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void ExportSvgIconButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                await ExportSvgAsFileAsync(SelectedItem);
            }
        }

        #endregion

        private void AnimateOpacity(UIElement target, double toValue)
        {
            var storyboard = new Storyboard();
            var animation = new DoubleAnimation { To = toValue, Duration = TimeSpan.FromMilliseconds(250), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private static T? FindVisualChildByTag<T>(DependencyObject? parent, string tag) where T : FrameworkElement
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Tag is string elementTag && elementTag == tag)
                {
                    return element;
                }
                var result = FindVisualChildByTag<T>(child, tag);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }

    public class SvgPathReceiver : ICanvasPathReceiver
    {
        private readonly StringBuilder _pathBuilder = new StringBuilder();

        public string GetPathData() => _pathBuilder.ToString();

        private string Format(float value)
        {
            return value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }

        public void BeginFigure(Vector2 startPoint, CanvasFigureFill fill)
        {
            _pathBuilder.Append($"M {Format(startPoint.X)} {Format(startPoint.Y)} ");
        }

        public void AddLine(Vector2 point)
        {
            _pathBuilder.Append($"L {Format(point.X)} {Format(point.Y)} ");
        }

        public void AddCubicBezier(Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint)
        {
            _pathBuilder.Append($"C {Format(controlPoint1.X)} {Format(controlPoint1.Y)}, {Format(controlPoint2.X)} {Format(controlPoint2.Y)}, {Format(endPoint.X)} {Format(endPoint.Y)} ");
        }

        public void AddQuadraticBezier(Vector2 controlPoint, Vector2 endPoint)
        {
            _pathBuilder.Append($"Q {Format(controlPoint.X)} {Format(controlPoint.Y)}, {Format(endPoint.X)} {Format(endPoint.Y)} ");
        }

        public void AddArc(Vector2 endPoint, float radiusX, float radiusY, float rotationAngle, CanvasSweepDirection sweepDirection, CanvasArcSize arcSize)
        {

            int sweep = sweepDirection == CanvasSweepDirection.Clockwise ? 1 : 0;
            int arc = arcSize == CanvasArcSize.Large ? 1 : 0;
            _pathBuilder.Append($"A {Format(radiusX)} {Format(radiusY)} {Format(rotationAngle)} {arc} {sweep} {Format(endPoint.X)} {Format(endPoint.Y)} ");
        }

        public void EndFigure(CanvasFigureLoop figureLoop)
        {
            if (figureLoop == CanvasFigureLoop.Closed)
            {
                _pathBuilder.Append("Z ");
            }
        }

        public void SetFilledRegionDetermination(CanvasFilledRegionDetermination filledRegionDetermination)
        {
            // Requerido por la interfaz ICanvasPathReceiver para Direct2D/DirectWrite
        }

        public void SetSegmentOptions(CanvasFigureSegmentOptions figureSegmentOptions)
        {
            // Requerido por la interfaz ICanvasPathReceiver para Direct2D/DirectWrite
        }
    }
}