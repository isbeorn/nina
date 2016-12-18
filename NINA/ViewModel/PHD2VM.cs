using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {
    class PHD2VM :BaseVM {
        public PHD2VM() {
            Name = "PHD2";

            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PHD2SVG"];            
            MaxY = 4;

            ConnectPHDClientCommand = new AsyncCommand<bool>(async () => await Task.Run<bool>(() => PHD2Client.connect()));
            DisconnectPHDClientCommand = new AsyncCommand<bool>(async () => await Task.Run<bool>(() => PHD2Client.disconnect()));
        }

        public PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }

        public double Interval {
            get {
                return MaxY / 4;
            }
        }

        public double MaxY {
            get {
                return _maxY;
            }

            set {
                _maxY = value;
                _minY = -value;
                RaisePropertyChanged("MinY");
                RaisePropertyChanged("Interval");
                RaisePropertyChanged();
            }
        }

        public double MinY {
            get {
                return _minY;
            }

            set {
                _minY = value;
                _maxY = -value;
                RaisePropertyChanged();
                RaisePropertyChanged("MaxY");
                RaisePropertyChanged("Interval");
            }
        }

        private AsyncCommand<bool> _connectPHDClientCommand;
        public AsyncCommand<bool> ConnectPHDClientCommand {
            get {
                return _connectPHDClientCommand;
            }

            set {
                _connectPHDClientCommand = value;
                RaisePropertyChanged();
            }
        }

        private Utility.AsyncCommand<bool> _disconnectPHDClientCommand;
        public Utility.AsyncCommand<bool> DisconnectPHDClientCommand {
            get {
                return _disconnectPHDClientCommand;
            }

            set {
                _disconnectPHDClientCommand = value;
                RaisePropertyChanged();
            }
        }



        private double _maxY;
        private double _minY;




    }
}
