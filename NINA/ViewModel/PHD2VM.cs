using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class PHD2VM : DockableVM {
        public PHD2VM() : base() {
            Title = "PHD2";
            ContentId = nameof(PHD2VM);
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PHD2SVG"];            
            ConnectPHDClientCommand = new AsyncCommand<bool>(async () => await Task.Run<bool>(() => Connect()));
            DisconnectPHDClientCommand = new AsyncCommand<bool>(async () => await Task.Run<bool>(() => PHD2Client.Disconnect()));

            PHD2Client.PropertyChanged += PHD2Client_PropertyChanged;

            /*SetUpPlotModels();*/

            MaxY = 4;

            GuideStepsHistory = new AsyncObservableCollection<PhdEventGuideStep>();
            GuideStepsHistoryMinimal = new AsyncObservableCollection<PhdEventGuideStep>();
        }

        private static Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;

        private async Task<bool> Connect() {


            GuideStepsHistory.Clear();
            GuideStepsHistoryMinimal.Clear();


            return await PHD2Client.Connect();
        }
        
        private void PHD2Client_PropertyChanged(object sender, PropertyChangedEventArgs e) {        
            if (e.PropertyName == "GuideStep") {

                
                if(GuideStepsHistory.Count > 100) {
                    GuideStepsHistory.RemoveAt(0);
                }
                if (GuideStepsHistoryMinimal.Count > 10) {
                    GuideStepsHistoryMinimal.RemoveAt(0);
                }

                GuideStepsHistoryMinimal.Add(PHD2Client.GuideStep);
                RaisePropertyChanged(nameof(GuideStepsHistoryMinimal));

                GuideStepsHistory.Add(PHD2Client.GuideStep);
                RaisePropertyChanged(nameof(GuideStepsHistory));
              
            }
        }

        

        public AsyncObservableCollection<PhdEventGuideStep> GuideStepsHistory { get; set; }
        public AsyncObservableCollection<PhdEventGuideStep> GuideStepsHistoryMinimal { get; set; }

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

        private double _maxY;
        public double MaxY {
            get {
                return _maxY;
            }

            set {
                _maxY = value;                
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(MinY));
            }
        }

        
        public double MinY {
            get {
                return -MaxY;
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
    }
}
