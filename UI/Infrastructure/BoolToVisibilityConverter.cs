using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI.Infrastructure
{
  public class BoolToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (bool)value
        ? Visibility.Collapsed
        : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var visibility = (Visibility)value;
      return visibility == Visibility.Collapsed || visibility == Visibility.Hidden;
    }
  }
}
