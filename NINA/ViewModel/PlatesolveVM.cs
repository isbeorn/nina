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
    class PlatesolveVM : DockableVM {

        public const string ASTROMETRYNETURL = "http://nova.astrometry.net";

        public PlatesolveVM() : base() {
            Title = "Plate Solving";
            ContentId = nameof(PlatesolveVM);
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];
            
            BlindSolveCommand = new AsyncCommand<bool>(() => BlindSolve(new Progress<string>(p => Status = p)));
            
            CancelBlindSolveCommand = new RelayCommand(CancelBlindSolve);
            SyncCommand = new RelayCommand(SyncTelescope);
            SyncAndReslewCommand = new RelayCommand(SyncTelescopeAndReslew);

            SnapExposureDuration = 2;


            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                Telescope = (ITelescope)o;
            }, MediatorMessages.TelescopeChanged);

            Mediator.Instance.Register((object o) => {
                Cam = (ICamera)o;
            }, MediatorMessages.CameraChanged);

            Mediator.Instance.Register((object o) => {
                Image = (BitmapSource)o;
            }, MediatorMessages.ImageChanged);

            Mediator.Instance.Register((object o) => {
                _autoStretch = (bool)o;
            }, MediatorMessages.AutoStrechChanged);
            Mediator.Instance.Register((object o) => {
                _detectStars = (bool)o;
            }, MediatorMessages.DetectStarsChanged);

            Mediator.Instance.RegisterAsync(async (object o) => {
                var args = (object[])o;

                double duration = (double)args[0];                
                IProgress<string> progress = (IProgress<string>)args[1];
                CancellationTokenSource token = (CancellationTokenSource)args[2];
                Model.MyFilterWheel.FilterInfo filter = (Model.MyFilterWheel.FilterInfo)args[3];
                BinningMode binning = (BinningMode)args[4];
                await BlindSolveWithCapture(duration, progress, token, filter, binning);
            }, AsyncMediatorMessages.BlindSolveWithCapture);

            Mediator.Instance.Register((object o) => {                
                sync();
            }, MediatorMessages.Sync);
        }

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();

                Mediator.Instance.Notify(MediatorMessages.StatusUpdate, _status);
            }
        }

        private void SyncTelescopeAndReslew(object obj) {
            if (Telescope?.Connected != true) {
                Notification.ShowWarning("Unable to sync. Telescope is not connected!");
                return;
            }
            
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
                Mediator.Instance.Notify(MediatorMessages.PlateSolveBinningChanged, _snapBin);
            }
        }

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }

            set {
                _snapFilter = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.PlateSolveFilterChanged, _snapFilter);
            }
        }

        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.PlateSolveExposureDurationChanged, _snapExposureDuration);
            }
        }

        private void ImageChanged(Object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Image") {
                this.PlateSolveResult = null;
            }
        }

        public bool sync() {
            var success = false;

            if(Telescope?.Connected != true) {
                Notification.ShowWarning("Unable to sync. Telescope is not connected!");
                return false;
            }

            if (PlateSolveResult != null) {

                Coordinates solved = new Coordinates(PlateSolveResult.Ra, PlateSolveResult.Dec, PlateSolveResult.Epoch, Coordinates.RAType.Degrees);
                solved = solved.Transform(Settings.EpochType);

                if (Telescope.Sync(solved.RA, solved.Dec) == true) {
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

        private BitmapSource _image;
        public BitmapSource Image {
            get {
                return _image;
            }
            set {
                _image = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoStretch;
        public bool AutoStretch {
            get {
                return _autoStretch;
            }
        }

        private bool _detectStars;
        public bool DetectStars {
            get {
                return _detectStars;
            }
        }

        public async Task<bool> BlindSolveWithCapture(double duration, IProgress<string> progress, CancellationTokenSource canceltoken, Model.MyFilterWheel.FilterInfo filter = null, Model.MyCamera.BinningMode binning = null) {
            var oldAutoStretch = AutoStretch;
            var oldDetectStars = DetectStars;
            Mediator.Instance.Notify(MediatorMessages.ChangeAutoStretch, true);
            Mediator.Instance.Notify(MediatorMessages.ChangeDetectStars, false);           

            await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.CaptureImage, new object[] { duration, false, progress, canceltoken, filter, binning });

            Mediator.Instance.Notify(MediatorMessages.ChangeAutoStretch, oldAutoStretch);
            Mediator.Instance.Notify(MediatorMessages.ChangeDetectStars, oldDetectStars);

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
               
                if(Settings.AnsvrSearchRadius > 0 && Telescope?.Connected == true) {
                    Platesolver = new LocalPlateSolver(Settings.AnsvrFocalLength, Settings.AnsvrPixelSize * Cam.BinX, Settings.AnsvrSearchRadius, new Coordinates(Telescope.RightAscension, Telescope.Declination, Settings.EpochType, Coordinates.RAType.Hours));
                } else {
                    Platesolver = new LocalPlateSolver(Settings.AnsvrFocalLength, Settings.AnsvrPixelSize * Cam.BinX);
                }
                
            }
            

            BitmapSource source = Image;
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
                if(!PlateSolveResult.Success) {
                    Notification.ShowWarning("Platesolve failed");
                }
                return PlateSolveResult.Success;
            });           
            
        }

        private CancellationTokenSource _blindeSolveCancelToken;
        private void CancelBlindSolve(object o) {
                _blindeSolveCancelToken?.Cancel();
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
                
        private ITelescope _telescope;
        public ITelescope Telescope {
            get {
                return _telescope;
            }
            set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private ICamera _cam;
        public ICamera Cam {
            get {
                return _cam;
            }
            set {
                _cam = value;
                RaisePropertyChanged();
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
                Mediator.Instance.Notify(MediatorMessages.PlateSolveResultChanged, _plateSolveResult);
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
