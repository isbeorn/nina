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
            SyncScope = false;
            SlewToTarget = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];

            SolveCommand = new AsyncCommand<bool>(() => CaputureAndSolve(new Progress<string>(p => Status = p)));

            CancelSolveCommand = new RelayCommand(CancelSolve);

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

                double duration = args[0] == null ? this.SnapExposureDuration : (double)args[0];
                IProgress<string> progress = (IProgress<string>)args[1];
                CancellationTokenSource token = (CancellationTokenSource)args[2];
                Model.MyFilterWheel.FilterInfo filter = args[3] == null ? this.SnapFilter : (Model.MyFilterWheel.FilterInfo)args[3];
                BinningMode binning = args[4] == null ? this.SnapBin : (BinningMode)args[4];
                await SolveWithCapture(duration, progress, token, filter, binning);
            }, AsyncMediatorMessages.SolveWithCapture);

            Mediator.Instance.RegisterAsync(async (object o) => {
                var args = (object[])o;                
                IProgress<string> progress = (IProgress<string>)args[0];
                CancellationTokenSource token = (CancellationTokenSource)args[1];                
                await Solve(progress, token);
            }, AsyncMediatorMessages.Solve);


            
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

        private bool _syncScope;
        public bool SyncScope {
            get {
                return _syncScope;
            }
            set {
                _syncScope = value;
                RaisePropertyChanged();
            }
        }

        private bool _slewToTarget;
        public bool SlewToTarget {
            get {
                return _slewToTarget;
            }
            set {
                _slewToTarget = value;
                if (_slewToTarget && !SyncScope) {
                    SyncScope = true;
                }
                RaisePropertyChanged();
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

            if (Telescope?.Connected != true) {
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

        public async Task<bool> SolveWithCapture(double duration, IProgress<string> progress, CancellationTokenSource canceltoken, Model.MyFilterWheel.FilterInfo filter = null, Model.MyCamera.BinningMode binning = null) {
            var oldAutoStretch = AutoStretch;
            var oldDetectStars = DetectStars;
            Mediator.Instance.Notify(MediatorMessages.ChangeAutoStretch, true);
            Mediator.Instance.Notify(MediatorMessages.ChangeDetectStars, false);

            await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.CaptureImage, new object[] { duration, false, progress, canceltoken, filter, binning });

            Mediator.Instance.Notify(MediatorMessages.ChangeAutoStretch, oldAutoStretch);
            Mediator.Instance.Notify(MediatorMessages.ChangeDetectStars, oldDetectStars);

            canceltoken.Token.ThrowIfCancellationRequested();

            return await Solve(progress, canceltoken); ;
        }


        private async Task<bool> CaputureAndSolve(IProgress<string> progress) {
            _solveCancelToken = new CancellationTokenSource();
            var solvesuccess = await SolveWithCapture(SnapExposureDuration, progress, _solveCancelToken, SnapFilter, SnapBin);
            
            PlateSolveResultList.Add(PlateSolveResult);
            if(solvesuccess) {
                CalculateError();
                if (SyncScope) {
                    if (Telescope?.Connected != true) {
                        Notification.ShowWarning("Unable to sync. Telescope is not connected!");
                        return false;
                    }
                    var coords = new Coordinates(Telescope.RightAscension, Telescope.Declination, Settings.EpochType, Coordinates.RAType.Hours);
                    if (sync() && SlewToTarget) {
                        Telescope.SlewToCoordinatesAsync(coords.RA, coords.Dec);
                    }
                }
            }
           
            
            RaiseAllPropertiesChanged();
            return solvesuccess;
        }

        private void CalculateError() {
            if (Telescope?.Connected == true) {

                Coordinates solved = new Coordinates(PlateSolveResult.Ra, PlateSolveResult.Dec, PlateSolveResult.Epoch, Coordinates.RAType.Degrees);
                solved = solved.Transform(Settings.EpochType);

                var coords = new Coordinates(Telescope.RightAscension, Telescope.Declination, Settings.EpochType, Coordinates.RAType.Hours);
                
                PlateSolveResult.RaError = coords.RADegrees - solved.RADegrees;
                PlateSolveResult.DecError = coords.Dec - solved.Dec;                
            }
        }

        public async Task<bool> Solve(IProgress<string> progress, CancellationTokenSource canceltoken) {
            if(Image != null) {

                
                Coordinates coords = null;                
                if (Telescope?.Connected == true) {
                    coords = new Coordinates(Telescope.RightAscension, Telescope.Declination, Settings.EpochType, Coordinates.RAType.Hours);
                }
                var binning = Cam?.BinX ?? 1;

                Platesolver = PlateSolverFactory.CreateInstance(binning, Image.Width, Image.Height, coords);
                
                if(Platesolver == null) {
                    return false;
                }
                
                BitmapSource source = Image;
                BitmapFrame image = null;
                
                image = BitmapFrame.Create(source);

                /* Read image into memorystream */
                var ms = new MemoryStream();
                JpegBitmapEncoder encoder = new JpegBitmapEncoder() { QualityLevel = 100 };
                encoder.Frames.Add(image);

                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                return await Task<bool>.Run(async () => {
                    PlateSolveResult = await Platesolver.SolveAsync(ms, progress, canceltoken);
                    if (!PlateSolveResult.Success) {
                        Notification.ShowWarning("Platesolve failed");
                    }
                    return PlateSolveResult.Success;
                });
            } else {
                return false;
            }
        }

        private CancellationTokenSource _solveCancelToken;
        private void CancelSolve(object o) {
            _solveCancelToken?.Cancel();
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

        private IAsyncCommand _solveCommand;
        public IAsyncCommand SolveCommand {
            get {
                return _solveCommand;
            }
            set {
                _solveCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _cancelSolveCommand;
        public ICommand CancelSolveCommand {
            get {
                return _cancelSolveCommand;
            }
            set {
                _cancelSolveCommand = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<PlateSolveResult> _plateSolveResultList;
        public AsyncObservableCollection<PlateSolveResult> PlateSolveResultList {
            get {
                if(_plateSolveResultList == null) {
                    _plateSolveResultList = new AsyncObservableCollection<PlateSolveResult>();
                }
                return _plateSolveResultList;
            }
            set {
                _plateSolveResultList = value;
                RaisePropertyChanged();
            }
        }

        private PlateSolveResult _plateSolveResult;
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
    }
}
