using NINA.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class PolarAlignmentVM : BaseVM {
        TelescopeModel _telescope;
        public TelescopeModel Telescope {
            get {
                return _telescope;
            }
            set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private double _rotation;
        public double Rotation {
            get {
                return _rotation;
            }
            set {
                _rotation = value;
                RaisePropertyChanged();
            }
        }

        public string HourAngleTime {
            get {
                return _hourAngleTime;
            }

            set {
                _hourAngleTime = value;
                RaisePropertyChanged();
            }
        }

        private string _hourAngleTime;
        

        DispatcherTimer _updateValues;

        public PolarAlignmentVM() {
            _updateValues = new DispatcherTimer();
            _updateValues.Interval = TimeSpan.FromSeconds(1);
            _updateValues.Tick += _updateValues_Tick;
            _updateValues.Start();
        }

        private void _updateValues_Tick(object sender, EventArgs e) {
            if(Telescope.Connected) {                
                var util = Utility.Utility.AstroUtils;
                var jd = util.JulianDateUtc;

                //J2000 Coordinates
                var polarisRA = Utility.Utility.AscomUtil.HMSToHours("02:31:49.09");
                var polarisDec = Utility.Utility.AscomUtil.DMSToDegrees("89:15:50.8");

                var NOVAS31 = Utility.Utility.NOVAS31;

                double[] coords = new double[4];
                double[] translatedcoords = new double[4];
                NOVAS31.RaDec2Vector(polarisRA, polarisDec, 2.738e+7, ref coords);
                //Convert J2000 to current coordinates
                NOVAS31.Precession(2451545.0, coords, jd, ref translatedcoords);
                NOVAS31.Vector2RaDec(translatedcoords, ref polarisRA, ref polarisDec);

                var hour_angle = Telescope.SiderealTime - polarisRA;

                Rotation = -(hour_angle / 24) * 360;
                HourAngleTime = Utility.Utility.AscomUtil.HoursToHMS(hour_angle);            
            }
        }
    }
}
