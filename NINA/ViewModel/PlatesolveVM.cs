using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
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
    class PlatesolveVM : ChildVM {

        public const string ASTROMETRYNETURL = "http://nova.astrometry.net";

        public PlatesolveVM(ApplicationVM root) : base(root) {
            
            this.ImagingVM = root.ImagingVM;
            this.TelescopeVM = root.TelescopeVM;

            Name = "Plate Solving";
            Progress = "Idle...";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];
            
            BlindSolveCommand = new AsyncCommand<bool>(() => BlindSolve(new Progress<string>(p => RootVM.Status = p)));
            
            CancelBlindSolveCommand = new RelayCommand(CancelBlindSolve);
            SyncCommand = new RelayCommand(SyncTelescope);
            SyncAndReslewCommand = new RelayCommand(SyncTelescopeAndReslew);

            SnapExposureDuration = 2;
                    
        }

        private void SyncTelescopeAndReslew(object obj) {
            var coords = new Coordinates(Telescope.RightAscension, Telescope.Declination, Settings.EpochType, Coordinates.RAType.Hours);

            if(sync()) {
                Telescope.SlewToCoordinatesAsync(coords.RA, coords.Dec);
            }
        }

        private BinningMode _snapBin;
        private Model.MyFilterWheel.FilterInfo _snapFilter;
        private double _snapExposureDuration;

        public BinningMode SnapBin {
            get {
                return _snapBin;
            }

            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }

            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private void ImageChanged(Object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Image") {
                this.PlateSolveResult = null;
            }
        }

        public bool sync() {
            var success = false;
            if (PlateSolveResult != null) {

                Coordinates solved = new Coordinates(PlateSolveResult.Ra, PlateSolveResult.Dec, PlateSolveResult.Epoch, Coordinates.RAType.Degrees);
                solved = solved.Transform(Settings.EpochType);

                if (Telescope.Sync(solved.RA, solved.Dec)) {
                    Notification.ShowSuccess("Telescope synced to coordinates");
                    success = true;
                } else {
                    Notification.ShowWarning("Telescope sync failed!");
                }

            } else {
                Notification.ShowWarning("No coordinates available to sync telescope!");
            }
            return success;
        }

        private void SyncTelescope(object obj) {
            sync();
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

        public async Task<bool> BlindSolveWithCapture(double duration, IProgress<string> progress, CancellationTokenSource canceltoken, Model.MyFilterWheel.FilterInfo filter = null, Model.MyCamera.BinningMode binning = null) {
            var oldAutoStretch = ImagingVM.ImageControl.AutoStretch;
            var oldDetectStars = ImagingVM.ImageControl.DetectStars;
            ImagingVM.ImageControl.AutoStretch = true;
            ImagingVM.ImageControl.DetectStars = false;
            await ImagingVM.CaptureImage(duration, false, progress, canceltoken, filter, binning);
            ImagingVM.ImageControl.DetectStars = oldDetectStars;
            ImagingVM.ImageControl.AutoStretch = oldAutoStretch;

            canceltoken.Token.ThrowIfCancellationRequested();
                        
            return await BlindSolve(progress, canceltoken); ;
        }
        

        private async Task<bool> BlindSolve(IProgress<string> progress) {
            _blindeSolveCancelToken = new CancellationTokenSource();
            return await BlindSolveWithCapture(SnapExposureDuration, progress, _blindeSolveCancelToken, SnapFilter, SnapBin);            
        }

        public async Task<bool> BlindSolve(IProgress<string> progress, CancellationTokenSource canceltoken) {
            bool fullresolution = true;
            if(Settings.PlateSolverType == PlateSolverEnum.ASTROMETRY_NET) {
                fullresolution = Settings.UseFullResolutionPlateSolve;
                Platesolver = new AstrometryPlateSolver(ASTROMETRYNETURL, Settings.AstrometryAPIKey);
            } else if (Settings.PlateSolverType == PlateSolverEnum.LOCAL) {
               
                if(Settings.AnsvrSearchRadius > 0 && Telescope != null && Telescope.Connected) {
                    Platesolver = new LocalPlateSolver(Settings.AnsvrFocalLength, Settings.AnsvrPixelSize * ImagingVM.Cam.BinX, Settings.AnsvrSearchRadius, new Coordinates(Telescope.RightAscension, Telescope.Declination, Settings.EpochType, Coordinates.RAType.Hours));
                } else {
                    Platesolver = new LocalPlateSolver(Settings.AnsvrFocalLength, Settings.AnsvrPixelSize * ImagingVM.Cam.BinX);
                }
                
            }
            

            BitmapSource source = ImagingVM.ImageControl.Image;
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
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(image);
            
            encoder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return await Task<bool>.Run(async () => {                
                PlateSolveResult = await Platesolver.BlindSolve(ms, progress, canceltoken);
                return PlateSolveResult.Success;
            });           
            
        }

        private CancellationTokenSource _blindeSolveCancelToken;
        private void CancelBlindSolve(object o) {
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
                ImagingVM.PropertyChanged += new PropertyChangedEventHandler(ImageChanged);
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
                
        public ITelescope Telescope {
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

        private ICommand _syncAndReslewCommand;
        public ICommand SyncAndReslewCommand {
            get {
                return _syncAndReslewCommand;
            }
            private set {
                _syncAndReslewCommand = value;
                RaisePropertyChanged();
            }
        }
    }
}
