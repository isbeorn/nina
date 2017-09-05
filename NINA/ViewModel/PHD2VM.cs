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
            Title = "LblPHD2";
            ContentId = nameof(PHD2VM);
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PHD2SVG"];            
            ConnectPHDClientCommand = new AsyncCommand<bool>(async () => await Task.Run<bool>(() => Connect()));
            DisconnectPHDClientCommand = new AsyncCommand<bool>(async () => await Task.Run<bool>(() => PHD2Client.Disconnect()));

            PHD2Client.PropertyChanged += PHD2Client_PropertyChanged;

            /*SetUpPlotModels();*/

            MaxY = 4;

            GuideStepsHistory = new AsyncObservableLimitedSizedStack<PhdEventGuideStep>(100);
            GuideStepsHistoryMinimal = new AsyncObservableLimitedSizedStack<PhdEventGuideStep>(10);
        }

        private static Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;

        private async Task<bool> Connect() {


            GuideStepsHistory.Clear();
            GuideStepsHistoryMinimal.Clear();


            return await PHD2Client.Connect();
        }
        
        private void PHD2Client_PropertyChanged(object sender, PropertyChangedEventArgs e) {        
            if (e.PropertyName == "GuideStep") {
                GuideStepsHistoryMinimal.Add(PHD2Client.GuideStep);
                GuideStepsHistory.Add(PHD2Client.GuideStep);              
            }
        }

        

        public AsyncObservableLimitedSizedStack<PhdEventGuideStep> GuideStepsHistory { get; set; }
        public AsyncObservableLimitedSizedStack<PhdEventGuideStep> GuideStepsHistoryMinimal { get; set; }

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

        
        public IAsyncCommand ConnectPHDClientCommand { get; private set; }
        
        public IAsyncCommand DisconnectPHDClientCommand { get; private set; }
    }
}
