using NINA.Utility;
using NINA.Utility.Astrometry;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.Model {
    public class DeepSkyObject:BaseINPC {

        public DeepSkyObject(string name) {
            Name = name;
            SetSequenceCoordinatesCommand = new RelayCommand(SetSequenceCoordinates);
            SlewToCoordinatesCommand = new RelayCommand(SlewToCoordinates);
        }

        private string _name;
        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates _coordinates;
        public Coordinates Coordinates {
            get {
                return _coordinates;
            }
            set {
                _coordinates = value;
                RaisePropertyChanged();
            }
        }

        private string _dSOType;
        public string DSOType {
            get {
                return _dSOType;
            }
            set {
                _dSOType = value;
                RaisePropertyChanged();
            }
        }

        private string _constellation;
        public string Constellation {
            get {
                return _constellation;
            }
            set {
                _constellation = value;
                RaisePropertyChanged();
            }
        }

        private double? _magnitude;
        public double? Magnitude {
            get {
                return _magnitude;
            }
            set {
                _magnitude = value;
                RaisePropertyChanged();
            }
        }
        private double? _size;
        public double? Size {
            get {
                return _size;
            }
            set {
                _size = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<KeyValuePair<DateTime,double>> _altitudes;
        public AsyncObservableCollection<KeyValuePair<DateTime,double>> Altitudes {
            get {
                if (_altitudes == null) {
                    _altitudes = new AsyncObservableCollection<KeyValuePair<DateTime,double>>();
                }
                return _altitudes;
            }
            set {
                _altitudes = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<string> _alsoKnownAs;
        public AsyncObservableCollection<string> AlsoKnownAs {
            get {
                if(_alsoKnownAs == null) {
                    _alsoKnownAs = new AsyncObservableCollection<string>();
                }
                return _alsoKnownAs;
            }
            set {
                _alsoKnownAs = value;
                RaisePropertyChanged();
            }
        }

        private void SetSequenceCoordinates(object obj) {
            Mediator.Instance.Notify(MediatorMessages.SetSequenceCoordinates,new object[] { AlsoKnownAs.FirstOrDefault(), Coordinates });
        }

        private void SlewToCoordinates(object obj) {
            Mediator.Instance.Notify(MediatorMessages.SlewToCoordinates,Coordinates);
        }

        public void CalculateElevation(DateTime start, double latitude,double longitude) {

            var siderealTime = Astrometry.GetLocalSiderealTime(start, longitude);            
            var hourAngle = Astrometry.GetHourAngle(siderealTime, this.Coordinates.RA);

            for (double angle = hourAngle;angle < hourAngle + 24;angle += 0.1) {
                var altitude = Astrometry.GetAltitude(angle,latitude, this.Coordinates.Dec);
                Altitudes.Add(new KeyValuePair<DateTime,double>(start,altitude));
                start = start.AddHours(0.1);
            }
        }

        public ICommand SetSequenceCoordinatesCommand { get; private set; }

        public ICommand SlewToCoordinatesCommand { get; private set; }
    }    
}
