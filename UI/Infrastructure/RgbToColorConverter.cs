using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace UI.Infrastructure
{
  public class RgbToColorConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (targetType != typeof(Color))
        return null;

      if (values.Any(v => v.GetType().FullName == "MS.Internal.NamedObject"))
        return Color.FromArgb(255, 170, 50, 50);

      byte r = System.Convert.ToByte(values[0]);
      byte g = System.Convert.ToByte(values[1]);
      byte b = System.Convert.ToByte(values[2]);

      return Color.FromRgb(r, g, b);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      object[] result = new object[3];

      Color color = (Color)value;

      result[0] = color.R;
      result[1] = color.G;
      result[2] = color.B;

      return result;
    }
  }
}
