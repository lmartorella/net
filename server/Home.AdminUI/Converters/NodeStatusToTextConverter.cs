using System;
using System.Globalization;
using System.Windows.Data;

namespace Lucky.Home.Converters
{
    public class NodeStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (NodeStatus)value;
            return status != null ? string.Format("[{0}]", ToString(status)) : "<nil>";
        }

        private static object ToString(NodeStatus status)
        {
            switch (status.ResetReason)
            {
                case ResetReason.Waiting:
                    return "???";
                case ResetReason.None:
                    return "None";
                case ResetReason.Power:
                    return "POW";
                case ResetReason.Brownout:
                    return "BRW";
                case ResetReason.ConfigMismatch:
                    return "CFG";
                case ResetReason.Watchdog:
                    return "WTD";
                case ResetReason.StackFail:
                    return "STK";
                case ResetReason.MClr:
                    return "RST";
                case ResetReason.Exception:
                    return "EXC: " + status.ExceptionMessage;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
