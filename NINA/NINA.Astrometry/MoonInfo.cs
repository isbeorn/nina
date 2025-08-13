using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Astrometry {
    public class MoonInfo : BaseINPC {

        public MoonInfo(Coordinates coordinates) {
            _coordinates = coordinates;
        }

        private Coordinates _coordinates;
        private ObserverInfo _observer;

        private DataPoint _maxMoonAltitude;

        public Coordinates Coordinates {
            get => _coordinates;
            set => _coordinates = value;
        }

        public DataPoint MaxAltitude {
            get => _maxMoonAltitude;
            set {
                _maxMoonAltitude = value;
                RaisePropertyChanged();
            }
        }

        private List<DataPoint> datapoints;
        public List<DataPoint> DataPoints => datapoints;

        private static Dictionary<DateTime, List<DataPoint>> Points = new Dictionary<DateTime, List<DataPoint>>();

        private DateTime _referenceDate = DateTime.MinValue;

        public void SetReferenceDateAndObserver(DateTime date, ObserverInfo observer) {
            _observer = observer;

            var calculate = DisplayMoon && date != _referenceDate;
            _referenceDate = date;
            if (calculate) {
                CalculateMoonData();
            } else {
                datapoints = null;
            }
            // Calculate separation in the middle of the chart period
            CalculateSeparation(date.AddHours(12));
        }

        private double _separation;

        public double Separation {
            get => _separation;
            set {
                _separation = value;
                SeparationText = Math.Round(Separation, 0).ToString().PadLeft(3, '0') + "°";
                RaisePropertyChanged();
            }
        }

        private string _separationText;

        public string SeparationText {
            get => _separationText;
            set {
                _separationText = value;
                RaisePropertyChanged();
            }
        }

        private bool _displayMoon;
        public bool DisplayMoon {
            get => _displayMoon;
            set {
                _displayMoon = value;
                if (value) {
                    CalculateMoonData();
                }
                RaisePropertyChanged();
            }
        }

        public AstroUtil.MoonPhase Phase => AstroUtil.GetMoonPhase(DateTime.Now);

        public Color Color {
            get {
                double angle = Math.Abs(AstroUtil.GetMoonPositionAngle(DateTime.Now));
                byte gray = (byte)(angle * 255 / 180);
                byte alpha = (byte)(255 - gray);
                return Color.FromArgb(alpha, gray, gray, gray);
            }
        }

        private static object lockObj = new object();
        private void CalculateMoonData() {
            lock(lockObj) { 
                if(_referenceDate == DateTime.MinValue) { return;  }
                if(!Points.ContainsKey(_referenceDate)) {
                    var list = new List<DataPoint>();

                    DateTime start = _referenceDate;

                    for (int i = 0; i < 24 * 10 /*24 hours x 10/hr*/; i++) {
                        list.Add(new DataPoint(DateTimeAxis.ToDouble(start), AstroUtil.GetMoonAltitude(start, _observer)));
                        start = start.AddHours(0.1);
                    }
                    Points.Add(_referenceDate, list);
                }

                datapoints = Points[_referenceDate];
                MaxAltitude = datapoints.OrderByDescending((x) => x.Y).FirstOrDefault();
                RaisePropertyChanged("DataPoints");
            }
        }

        private void CalculateSeparation(DateTime time) {
            NOVAS.SkyPosition pos = AstroUtil.GetMoonPosition(time, AstroUtil.GetJulianDate(time), _observer);
            var moonRaRadians = AstroUtil.ToRadians(AstroUtil.HoursToDegrees(pos.RA));
            var moonDecRadians = AstroUtil.ToRadians(pos.Dec);

            Coordinates target = _coordinates.Transform(Epoch.JNOW);
            var targetRaRadians = AstroUtil.ToRadians(target.RADegrees);
            var targetDecRadians = AstroUtil.ToRadians(target.Dec);

            var theta = SOFA.Seps(moonRaRadians, moonDecRadians, targetRaRadians, targetDecRadians);
            Separation = AstroUtil.ToDegree(theta);
        }
    }
}
