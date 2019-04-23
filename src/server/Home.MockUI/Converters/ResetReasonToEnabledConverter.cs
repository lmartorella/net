using System;
using System.Globalization;
using System.Windows.Data;
using Lucky.Home;

namespace Lucky.HomeMock.Converters
{
    public class ResetReasonToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ResetReason)value) == ResetReason.Exception;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
