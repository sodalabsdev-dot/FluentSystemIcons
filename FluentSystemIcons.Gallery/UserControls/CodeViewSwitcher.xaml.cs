using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using Windows.Foundation;

namespace FluentSystemIcons.Gallery.UserControls
{
    public enum CodeViewMode
    {
        Xaml,
        CSharp
    }

    public sealed partial class CodeViewSwitcher : UserControl
    {

        public event TypedEventHandler<CodeViewSwitcher, CodeViewMode>? SelectionChanged;

        public static readonly DependencyProperty CurrentViewModeProperty =
            DependencyProperty.Register(nameof(CurrentViewMode), typeof(CodeViewMode), typeof(CodeViewSwitcher),
                new PropertyMetadata(CodeViewMode.Xaml, OnCurrentViewModeChanged));

        public CodeViewMode CurrentViewMode
        {
            get => (CodeViewMode)GetValue(CurrentViewModeProperty);
            set => SetValue(CurrentViewModeProperty, value);
        }

        private bool _isInitialLayoutApplied = false;

        public CodeViewSwitcher()
        {
            this.InitializeComponent();
            this.SizeChanged += OnSwitcherSizeChanged;
        }

        private void OnSwitcherSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isInitialLayoutApplied && e.NewSize.Width > 0)
            {
                UpdateSelectionIndicator(false);
                _isInitialLayoutApplied = true;
            }
        }

        private static void OnCurrentViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CodeViewSwitcher control)
            {
                if (control._isInitialLayoutApplied)
                {
                    control.UpdateSelectionIndicator(true);
                }
                control.SelectionChanged?.Invoke(control, (CodeViewMode)e.NewValue);
            }
        }

        private void OnViewButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string viewName } && Enum.TryParse<CodeViewMode>(viewName, out var newMode))
            {
                CurrentViewMode = newMode;
            }
        }

        private void UpdateSelectionIndicator(bool animate)
        {
            if (SwitcherGrid.ActualWidth == 0) return;

            int selectedIndex = (int)this.CurrentViewMode;
            double availableWidth = SwitcherGrid.ActualWidth - SwitcherGrid.Padding.Left - SwitcherGrid.Padding.Right;
            double tabWidth = availableWidth / 2;
            double targetX = tabWidth * selectedIndex;

            if (animate)
            {
                var storyboard = new Storyboard();
                var animation = new DoubleAnimation
                {
                    To = targetX,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(animation, IndicatorTransform);
                Storyboard.SetTargetProperty(animation, "X");
                storyboard.Children.Add(animation);
                storyboard.Begin();
            }
            else
            {
                IndicatorTransform.X = targetX;
            }
        }
    }
}