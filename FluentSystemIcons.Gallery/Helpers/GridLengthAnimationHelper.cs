using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentSystemIcons.Gallery.Helpers
{
    public class GridLengthAnimationHelper : FrameworkElement
    {
        public static readonly DependencyProperty AnimatedValueProperty =
            DependencyProperty.Register(nameof(AnimatedValue), typeof(double), typeof(GridLengthAnimationHelper),
            new PropertyMetadata(0.0, OnAnimatedValueChanged));

        public double AnimatedValue
        {
            get { return (double)GetValue(AnimatedValueProperty); }
            set { SetValue(AnimatedValueProperty, value); }
        }

        public ColumnDefinition? TargetColumn { get; set; }

        private static void OnAnimatedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var helper = (GridLengthAnimationHelper)d;
            if (helper.TargetColumn != null)
            {
                var newWidth = (double)e.NewValue;
                helper.TargetColumn.Width = new GridLength(newWidth);
            }
        }
    }
}