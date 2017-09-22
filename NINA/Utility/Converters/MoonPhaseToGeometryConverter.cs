using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.Utility.Converters {
    public class MoonPhaseToGeometryConverter:IValueConverter {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
            Astrometry.Astrometry.MoonPhase phase = (Astrometry.Astrometry.MoonPhase) value;
            if(phase == Astrometry.Astrometry.MoonPhase.NewMoon) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["NewMoonSVG"];
            } else if (phase == Astrometry.Astrometry.MoonPhase.FirstQuarter) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FirstQuarterMoonSVG"];
            }
            else if (phase == Astrometry.Astrometry.MoonPhase.FullMoon) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FullMoonSVG"];
            }
            else if (phase == Astrometry.Astrometry.MoonPhase.LastQuarter) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["LastQuarterMoonSVG"];
            }
            else if (phase == Astrometry.Astrometry.MoonPhase.WaningCrescent) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaningCrescentMoonSVG"];
            }
            else if (phase == Astrometry.Astrometry.MoonPhase.WaningGibbous) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaningGibbousMoonSVG"];
            }
            else if (phase == Astrometry.Astrometry.MoonPhase.WaxingCrescent) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaxingCrescentMoonSVG"];
            }
            else if (phase == Astrometry.Astrometry.MoonPhase.WaxingGibbous) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaxingGibbousMoonSVG"];
            } else {
                return null;
            }
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
