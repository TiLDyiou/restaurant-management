using System.Globalization;

namespace RestaurantManagementGUI.Converters
{
    public class ImageUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = value as string;
            if (string.IsNullOrEmpty(url))
                return null;
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                return url.Replace("localhost", "10.0.2.2")
                          .Replace("127.0.0.1", "10.0.2.2");
            }

            return url;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

/*
using System.Globalization;

namespace RestaurantManagementGUI.Converters
{
    public class ImageUrlConverter : IValueConverter
    {
        const string DOMAIN = "http://qlnhnhom2.runasp.net/";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = value as string;
            if (string.IsNullOrEmpty(url)) return null;
            if (!url.StartsWith("http"))
            {
                return Path.Combine(DOMAIN, url).Replace("\\", "/");
            }
            if (url.Contains("localhost") || url.Contains("10.0.2.2"))
            {
                return url.Replace("localhost:5276", "http://qlnhnhom2.runasp.net/")
                          .Replace("10.0.2.2:5276", "http://qlnhnhom2.runasp.net/")
                          .Replace("localhost", "http://qlnhnhom2.runasp.net/")
                          .Replace("10.0.2.2", "http://qlnhnhom2.runasp.net/")
                          .Replace("https://", "http://");
            }

            return url;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

*/