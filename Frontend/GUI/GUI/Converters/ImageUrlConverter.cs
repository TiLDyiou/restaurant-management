using System.Globalization;
using RestaurantManagementGUI.Helpers;
using Microsoft.Maui.Devices;

namespace RestaurantManagementGUI.Converters
{
    public class ImageUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = value as string;

            if (string.IsNullOrEmpty(url))
                return "placeholder_image.png";

            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (DeviceInfo.Platform == DevicePlatform.Android && url.Contains("localhost"))
                {
                    return url.Replace("localhost", "10.0.2.2");
                }
                return url;
            }

            string baseDomain = ApiConfig.BaseUrl;

            baseDomain = baseDomain.TrimEnd('/');

            if (url.StartsWith("/"))
                url = url.Substring(1);

            return $"{baseDomain}/{url}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}