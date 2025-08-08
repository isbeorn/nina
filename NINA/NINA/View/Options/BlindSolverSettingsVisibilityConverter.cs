using NINA.Core.Enum;
using NINA.PlateSolving.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NINA.View.Options {
    internal class BlindSolverSettingsVisibilityConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length == 2) {
                var solver = (PlateSolverEnum)values[0];
                var blindSolver = (BlindSolverEnum)values[1];

                if (solver.ToString() == blindSolver.ToString()) {
                    return Visibility.Collapsed;
                }
            }
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
