using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using Windows.Foundation;

namespace FluentSystemIcons.Gallery.UserControls
{
    public enum GridSizeMode
    {
        Large,
        Medium,
        Small
    }

    public sealed partial class GridSizeSwitcher : UserControl
    {
        public event TypedEventHandler<GridSizeSwitcher, GridSizeMode>? SelectionChanged;

        public static readonly DependencyProperty CurrentSizeModeProperty =
            DependencyProperty.Register(nameof(CurrentSizeMode), typeof(GridSizeMode), typeof(GridSizeSwitcher),
                new PropertyMetadata(GridSizeMode.Medium, OnCurrentSizeModeChanged));
        public GridSizeMode CurrentSizeMode
        {
            get => (GridSizeMode)GetValue(CurrentSizeModeProperty);
            set => SetValue(CurrentSizeModeProperty, value);
        }

        private bool _isInitialLayoutApplied = false;

        public GridSizeSwitcher()
        {
            this.InitializeComponent();
            this.SizeChanged += OnGridSizeSwitcherSizeChanged;
        }
        private void OnGridSizeSwitcherSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isInitialLayoutApplied && e.NewSize.Width > 0)
            {
                UpdateSelectionIndicator(false);
                _isInitialLayoutApplied = true;
            }
        }

        private static void OnCurrentSizeModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridSizeSwitcher control)
            {
                if (control._isInitialLayoutApplied)
                {
                    control.UpdateSelectionIndicator(true);
                }
                control.SelectionChanged?.Invoke(control, (GridSizeMode)e.NewValue);
            }
        }

        private void OnSizeButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedButton || clickedButton.Tag is not string sizeName)
                return;
            if (Enum.TryParse<GridSizeMode>(sizeName, out GridSizeMode newMode))
            {
                CurrentSizeMode = newMode;
            }
        }

        private void UpdateSelectionIndicator(bool animate)
        {
            if (GridSizeSwitcherGrid.ActualWidth == 0) return;
            int selectedIndex = (int)this.CurrentSizeMode;
            double availableWidth = GridSizeSwitcherGrid.ActualWidth - GridSizeSwitcherGrid.Padding.Left - GridSizeSwitcherGrid.Padding.Right;
            double tabWidth = availableWidth / 3;
            double targetBackgroundX = tabWidth * selectedIndex;
            if (animate)
            {
                var storyboard = new Storyboard();
                var duration = new Duration(TimeSpan.FromMilliseconds(300));
                var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
                var backgroundAnimation = new DoubleAnimation
                {
                    To = targetBackgroundX,
                    Duration = duration,
                    EasingFunction = easing
                };
                Storyboard.SetTarget(backgroundAnimation, BackgroundTranslateTransform);
                Storyboard.SetTargetProperty(backgroundAnimation, "X");
                storyboard.Children.Add(backgroundAnimation);
                storyboard.Begin();
            }
            else
            {
                BackgroundTranslateTransform.X = targetBackgroundX;
            }
        }
    }
}