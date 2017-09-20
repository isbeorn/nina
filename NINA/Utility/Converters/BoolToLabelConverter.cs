using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.Utility.Converters
{
    public class BoolToLabelConverter:IValueConverter {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
            string parameterString = parameter as string;
            if (!string.IsNullOrEmpty(parameterString)) {
                string[] parameters = parameterString.Split(new char[] { '|' });
                if(parameters.Length != 2) {
                    throw new Exception("Two Parameters required. Must be separated by |");
                }
                if((bool) value) {
                    return Locale.Loc.Instance[parameters[0]];
                } else {
                    return Locale.Loc.Instance[parameters[1]];
                }

            } else {
                throw new Exception("Two Parameters required. Must be separated by |");
            }

        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
