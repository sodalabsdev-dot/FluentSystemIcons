using FluentSystemIcons.Gallery.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace FluentSystemIcons.Gallery.Converters
{
    public class SelectedCodeViewToVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {

            if (value is CodeViewMode current && parameter is string tag &&
                Enum.TryParse<CodeViewMode>(tag, out var mode))
            {
                return current == mode ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class UnselectedCodeViewToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is CodeViewMode current && parameter is string tag &&
                Enum.TryParse<CodeViewMode>(tag, out var mode))
            {
                return current != mode ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}