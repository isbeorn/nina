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
    class PHD2VM :BaseVM {
        public PHD2VM() {
            Name = "PHD2";

            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PHD2SVG"];            
            ConnectPHDClientCommand = new AsyncCommand<bool>(async () => await Task.Run<bool>(() => connect()));
            DisconnectPHDClientCommand = new AsyncCommand<bool>(async () => await Task.Run<bool>(() => PHD2Client.disconnect()));

            PHD2Client.PropertyChanged += PHD2Client_PropertyChanged;

            /*SetUpPlotModels();*/

            MaxY = 4;

            GuideStepsHistory = new ObservableCollection<PhdEventGuideStep>();
        }

        private static Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        private async Task<bool> connect() {
            /*SetUpPlotModels();*/
            await dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                GuideStepsHistory.Clear();
            }));
            return await PHD2Client.connect();
        }

        /*private void SetUpPlotModels() {

            GuideSteps = new PlotModel {
                PlotAreaBorderColor = OxyColor.FromArgb(Settings.BorderColor.A, Settings.BorderColor.R, Settings.BorderColor.G, Settings.BorderColor.B),
                LegendBorder = OxyColor.FromArgb(Settings.BorderColor.A, Settings.BorderColor.R, Settings.BorderColor.G, Settings.BorderColor.B),
                LegendBackground = OxyColor.FromArgb(Settings.BackgroundColor.A, Settings.BackgroundColor.R, Settings.BackgroundColor.G, Settings.BackgroundColor.B),
                LegendTextColor = OxyColor.FromArgb(Settings.PrimaryColor.A, Settings.PrimaryColor.R, Settings.PrimaryColor.G, Settings.PrimaryColor.B),
                LegendPosition = LegendPosition.BottomLeft,
                LegendOrientation = LegendOrientation.Vertical
            };
            GuideSteps.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left ,
                Minimum = -MaxY,
                Maximum = MaxY,
                MajorGridlineStyle = LineStyle.LongDash,
                MinorGridlineStyle = LineStyle.Dash,
                IntervalLength = 50,
                TextColor = OxyColor.FromArgb(Settings.PrimaryColor.A, Settings.PrimaryColor.R, Settings.PrimaryColor.G, Settings.PrimaryColor.B),
                AxislineColor = OxyColor.FromArgb(Settings.PrimaryColor.A, Settings.PrimaryColor.R, Settings.PrimaryColor.G, Settings.PrimaryColor.B),
                MajorGridlineColor = OxyColor.FromArgb(100, Settings.PrimaryColor.R, Settings.PrimaryColor.G, Settings.PrimaryColor.B),
                MinorGridlineColor = OxyColor.FromArgb(50, Settings.PrimaryColor.R, Settings.PrimaryColor.G, Settings.PrimaryColor.B),
            });
            GuideSteps.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, IsAxisVisible = false });
            GuideSteps.Series.Add(new LineSeries { Title="RA", LineStyle = LineStyle.Solid, Color = OxyColors.Blue });            
            GuideSteps.Series.Add(new LineSeries { Title = "Dec", LineStyle = LineStyle.Solid, Color = OxyColors.Red });

            GuideSteps.Series.Add(new LinearBarSeries { Title = "RACorrections", StrokeColor = OxyColors.Blue, StrokeThickness = 1, FillColor = OxyColors.Transparent });
            GuideSteps.Series.Add(new LinearBarSeries { Title = "DecCorrections", StrokeColor = OxyColors.Red, StrokeThickness = 1, FillColor = OxyColors.Transparent });

            RaisePropertyChanged("GuideSteps");            
        }*/

        private void PHD2Client_PropertyChanged(object sender, PropertyChangedEventArgs e) {        
            if (e.PropertyName == "GuideStep") {

                dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    if(GuideStepsHistory.Count > 100) {
                        GuideStepsHistory.RemoveAt(0);
                    }
                    GuideStepsHistory.Add(PHD2Client.GuideStep);
                    RaisePropertyChanged("GuideStepsHistory");
                }));
              
                

                /*var ls = (LineSeries)GuideSteps.Series[0];
                
                double x = PHD2Client.GuideStep.Time;
                if (ls.Points.Count >= 100)
                    ls.Points.RemoveAt(0);

                ls.Points.Add(new DataPoint(x, PHD2Client.GuideStep.RADistanceRaw));


                ls = (LineSeries)GuideSteps.Series[1];                
                if (ls.Points.Count >= 100)
                    ls.Points.RemoveAt(0);

                ls.Points.Add(new DataPoint(x, -PHD2Client.GuideStep.DecDistanceRaw));

                var lbs = (LinearBarSeries)GuideSteps.Series[2];
                if (lbs.Points.Count >= 100)
                   lbs.Points.RemoveAt(0);

                lbs.Points.Add(new DataPoint(x-0.2, PHD2Client.GuideStep.RADistanceGuide));

                lbs = (LinearBarSeries)GuideSteps.Series[3];
                if (lbs.Points.Count >= 100)
                    lbs.Points.RemoveAt(0);

                lbs.Points.Add(new DataPoint(x+0.2, -PHD2Client.GuideStep.DecDistanceGuide));

                GuideSteps.InvalidatePlot(true);*/
            }
                
        
        }

        /*public PlotModel GuideSteps { get; set; }*/

        public ObservableCollection<PhdEventGuideStep> GuideStepsHistory { get; set; }

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
                /*if(GuideSteps != null && GuideSteps.Axes.Count > 0) { 
                    GuideSteps.Axes[0].Maximum = MaxY;
                    GuideSteps.Axes[0].Minimum = -MaxY;
                }*/
                RaisePropertyChanged();
                RaisePropertyChanged("MinY");
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
