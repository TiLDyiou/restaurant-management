// File: Converters/ChatConverters.cs
using System.Globalization;

namespace RestaurantManagementGUI.Converters
{
    /// <summary>
    /// Converter để đảo ngược giá trị bool - cho Chat
    /// </summary>
    public class ChatInvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }

    /// <summary>
    /// Converter để check object có null không - cho Chat
    /// </summary>
    public class ChatIsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để chuyển bool thành màu - cho Chat
    /// </summary>
    public class ChatBoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && parameter is string colors)
            {
                var colorArray = colors.Split('|');
                if (colorArray.Length == 2)
                {
                    return Color.FromArgb(isSelected ? colorArray[0] : colorArray[1]);
                }
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để chuyển bool thành opacity (0.5 nếu false, 1.0 nếu true)
    /// </summary>
    public class ChatBoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
                return isEnabled ? 1.0 : 0.5;
            return 0.5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}