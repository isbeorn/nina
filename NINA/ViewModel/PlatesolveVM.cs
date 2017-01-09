using NINA.Model;
using NINA.PlateSolving;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {
    class PlatesolveVM : BaseVM {

        public PlatesolveVM() {
            Name = "Plate Solving";
            Progress = "Idle...";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];
            
            BlindSolveCommand = new AsyncCommand<bool>(() => blindSolve(new Progress<string>(p => Progress = p)));
            CancelBlindSolveCommand = new RelayCommand(cancelBlindSolve);
            SyncCommand = new RelayCommand(syncTelescope);

            
                    
        }

        private void imageChanged(Object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Image") {
                this.PlateSolveResult = null;
            }
        }

        private void syncTelescope(object obj) {
            if(PlateSolveResult != null) {
                
                Coordinates solved = new Coordinates(PlateSolveResult.Ra, PlateSolveResult.Dec, PlateSolveResult.Epoch, Coordinates.RAType.Degrees);
                solved = solved.transform(Settings.EpochType);

                if (Telescope.sync(solved.RA, solved.Dec)) {
                    Notification.ShowSuccess("Telescope synced to coordinates");
                } else {
                    Notification.ShowWarning("Telescope sync failed!");
                }

            } else {
                Notification.ShowWarning("No coordinates available to sync telescope!");
            }
        }

        private string _progress;
        public string Progress {
            get {
                return _progress;
            }
            set {
                _progress = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> blindSolveWithCapture(IProgress<string> progress, CancellationTokenSource canceltoken) {
            var oldAutoStretch = ImagingVM.AutoStretch;
            ImagingVM.AutoStretch = true;          
            await ImagingVM.captureImage(2, false, progress, canceltoken);
            ImagingVM.AutoStretch = oldAutoStretch;

            canceltoken.Token.ThrowIfCancellationRequested();
            
            await blindSolve(progress, canceltoken);
            return true;
        }
        

        private async Task<bool> blindSolve(IProgress<string> progress) {
            _blindeSolveCancelToken = new CancellationTokenSource();
            return await blindSolve(progress, _blindeSolveCancelToken);
        }

        public async Task<bool> blindSolve(IProgress<string> progress, CancellationTokenSource canceltoken) {
            bool fullresolution = true;
            if(Settings.PlateSolverType == PlateSolverEnum.ASTROMETRY_NET) {
                fullresolution = Settings.UseFullResolutionPlateSolve;
                Platesolver = new AstrometryPlateSolver("http://nova.astrometry.net", Settings.AstrometryAPIKey);
            } else if (Settings.PlateSolverType == PlateSolverEnum.LOCAL) {
               
                Platesolver = new LocalPlateSolver(Settings.AnsvrFocalLength, Settings.AnsvrPixelSize * ImagingVM.Cam.BinX);
            }
            

            BitmapSource source = ImagingVM.Image;
            BitmapFrame image = null;
            /* Resize Image */
            if (!fullresolution && source.Width > 1400) {
                var factor = 1400 / source.Width;
                int width = (int)(source.Width * factor);
                int height = (int)(source.Height * factor);
                var margin = 0;
                var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

                var group = new DrawingGroup();
                RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
                group.Children.Add(new ImageDrawing(source, rect));

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                    drawingContext.DrawDrawing(group);

                var resizedImage = new RenderTargetBitmap(
                    width, height,         // Resized dimensions
                    96, 96,                // Default DPI values
                    PixelFormats.Default); // Default pixel format
                resizedImage.Render(drawingVisual);

                image = BitmapFrame.Create(resizedImage);
            }
            else {
                image = BitmapFrame.Create(source);
            }



            /* Read image into memorystream */
            var ms = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(image);
            encoder.QualityLevel = 100;
            encoder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return await Task<bool>.Run(async () => {

                _blindeSolveCancelToken = new CancellationTokenSource();
                PlateSolveResult = await Platesolver.blindSolve(ms, progress, _blindeSolveCancelToken);
                return PlateSolveResult.Success;
            });           
            
        }

        private CancellationTokenSource _blindeSolveCancelToken;
        private void cancelBlindSolve(object o) {
            if (_blindeSolveCancelToken != null) {
                _blindeSolveCancelToken.Cancel();
            }
        }

        IPlateSolver _platesolver;
        IPlateSolver Platesolver {
            get {
                return _platesolver;
            }
            set {
                _platesolver = value;
                RaisePropertyChanged();
            }

        }

        private PlateSolveResult _plateSolveResult;

        private ImagingVM _imagingVM;
        public ImagingVM ImagingVM {
            get {
                return _imagingVM;
            }
            set {
                _imagingVM = value;
                RaisePropertyChanged();
                ImagingVM.PropertyChanged += new PropertyChangedEventHandler(imageChanged);
            }
        }

        private TelescopeVM _telescopeVM;
        public TelescopeVM TelescopeVM {
            get {
                return _telescopeVM;
            } set {
                _telescopeVM = value;
                RaisePropertyChanged();
            }
        }
                
        public TelescopeModel Telescope {
            get {
                return TelescopeVM.Telescope;
            }
        }

        private IAsyncCommand _blindSolveCommand;
        public IAsyncCommand BlindSolveCommand {
            get {
                return _blindSolveCommand;
            }
            set {
                _blindSolveCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _cancelBlindSolveCommand;
        public ICommand CancelBlindSolveCommand {
            get {
                return _cancelBlindSolveCommand;
            }
            set {
                _cancelBlindSolveCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _syncCommand;
        public ICommand SyncCommand {
            get {
                return _syncCommand;
            }
            set {
                _syncCommand = value;
                RaisePropertyChanged();
            }
        }

        public PlateSolveResult PlateSolveResult {
            get {
                return _plateSolveResult;
            }

            set {
                _plateSolveResult = value;
                RaisePropertyChanged();
            }
        }
    }
}
