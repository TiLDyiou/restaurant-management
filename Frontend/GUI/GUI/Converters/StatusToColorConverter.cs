using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantManagementGUI.Converters
{
    internal class StatusToColorConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            return status switch
            {
                "Bàn trống" => Colors.Green,
                "Bàn bận" => Color.FromArgb("#503106"), 
                "Bàn đã đặt" => Colors.Orange,
                _ => Colors.Transparent 
            };
        }
    }
}
