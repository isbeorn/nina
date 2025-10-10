using NINA.Core.Locale;
using NINA.Sequencer;
using NINA.Sequencer.Logic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace NINA.View.Sequencer.Converter {
    public class SymbolToTooltipConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            // values[0] = Symbol, values[1] = DataContext (root VM)
            var symbol = values[0] as Symbol;
            if (symbol is null || values[1] is null) return null;

            if (values[1] is SymbolController vm) {
                if (symbol.Constants != null) {
                    StringBuilder sb = new StringBuilder(Loc.Instance["Lbl_SymbolBroker_SymbolOptions"] + " ");
                    Symbol[] cList = symbol.Constants;
                    for (int i = 0; i < cList.Length; i++) {
                        sb.Append(cList[i].Key);
                        if (i != cList.Length - 1) {
                            sb.Append("; ");
                        }
                    }
                    return sb.ToString();
                }
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
