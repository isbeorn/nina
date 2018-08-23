using NINA.Model;
using NINA.Model.MyCamera;
using NINA.PlateSolving;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Behaviors;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using NINA.Utility.SkySurvey;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace NINA.ViewModel {

    internal class FramingAssistantVM : BaseVM, ICameraConsumer {

        public FramingAssistantVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IImagingMediator imagingMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.telescopeMediator = telescopeMediator;
            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            Coordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);
            DSO = new DeepSkyObject(string.Empty, Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            //Coordinates = new Coordinates(073.2920, -07.6335, Epoch.J2000, Coordinates.RAType.Degrees);
            //Coordinates = new Coordinates(10.6833, 41.2686, Epoch.J2000, Coordinates.RAType.Degrees);

            CameraPixelSize = profileService.ActiveProfile.CameraSettings.PixelSize;
            FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength;

            _statusUpdate = new Progress<ApplicationStatus>(p => Status = p);

            LoadImageCommand = new AsyncCommand<bool>(async () => { return await LoadImage(); });
            CancelLoadImageFromFileCommand = new RelayCommand((object o) => { CancelLoadImage(); });
            _progress = new Progress<int>((p) => DownloadProgressValue = p);
            CancelLoadImageCommand = new RelayCommand((object o) => { CancelLoadImage(); });
            DragStartCommand = new RelayCommand(DragStart);
            DragStopCommand = new RelayCommand(DragStop);
            DragMoveCommand = new RelayCommand(DragMove);
            ClearCacheCommand = new RelayCommand(ClearCache);
            SetSequenceCoordinatesCommand = new AsyncCommand<bool>(async () => {
                //todo
                var vm = (ApplicationVM)Application.Current.Resources["AppVM"];
                vm.ChangeTab(ApplicationTab.SEQUENCE);
                var dso = new DeepSkyObject(DSO?.Name, SelectedCoordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                var msgResult = await vm.SeqVM.SetSequenceCoordiantes(dso);

                ImageParameter = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return msgResult;
            }, (object o) => SelectedCoordinates != null);

            RecenterCommand = new AsyncCommand<bool>(async () => {
                Coordinates = SelectedCoordinates;
                await LoadImageCommand.ExecuteAsync(null);
                return true;
            }, (object o) => SelectedCoordinates != null);

            SlewToCoordinatesCommand = new AsyncCommand<bool>(async () => {
                if (SelectedCoordinates != null) {
                    return await telescopeMediator.SlewToCoordinatesAsync(SelectedCoordinates);
                }
                return false;
            }, (object o) => SelectedCoordinates != null);

            SelectedImageCacheInfo = (XElement)ImageCacheInfo.FirstNode;

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RaisePropertyChanged(nameof(CameraPixelSize));
                RaisePropertyChanged(nameof(FocalLength));
                RaisePropertyChanged(nameof(FieldOfView));
                RaisePropertyChanged(nameof(CameraWidth));
                RaisePropertyChanged(nameof(CameraHeight));
            };

            profileService.LocationChanged += (object sender, EventArgs e) => {
                DSO = new DeepSkyObject(DSO.Name, DSO.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            };
        }

        private ISkySurveyFactory skySurveyFactory;

        public ISkySurveyFactory SkySurveyFactory {
            get {
                if (skySurveyFactory == null) {
                    skySurveyFactory = new SkySurveyFactory();
                }
                return skySurveyFactory;
            }
            set {
                skySurveyFactory = value;
            }
        }

        private void ClearCache(object obj) {
            var diagResult = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblClearCache"] + "?", "", MessageBoxButton.YesNo, MessageBoxResult.No);
            if (diagResult == MessageBoxResult.Yes) {
                CacheSkySurvey.Clear();
                RaisePropertyChanged(nameof(ImageCacheInfo));
            }
        }

        public static string FRAMINGASSISTANTCACHEPATH = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "FramingAssistantCache");
        public static string FRAMINGASSISTANTCACHEINFOPATH = Path.Combine(FRAMINGASSISTANTCACHEPATH, "CacheInfo.xml");

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = Locale.Loc.Instance["LblFramingAssistant"];
                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public async Task<bool> SetCoordinates(DeepSkyObject dso) {
            this.DSO = new DeepSkyObject(dso.Name, dso.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            this.Coordinates = dso.Coordinates;
            FramingAssistantSource = SkySurveySource.NASA;
            await LoadImageCommand.ExecuteAsync(null);
            return true;
        }

        private void CancelLoadImage() {
            _loadImageSource?.Cancel();
        }

        private Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        private DeepSkyObject _dSO;

        public DeepSkyObject DSO {
            get {
                return _dSO;
            }
            set {
                _dSO = value;
                _dSO?.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                RaisePropertyChanged();
            }
        }

        private Coordinates _coordinates;
        private ICameraMediator cameraMediator;
        private ITelescopeMediator telescopeMediator;
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        public Coordinates Coordinates {
            get {
                return _coordinates;
            }
            set {
                _coordinates = value;
                RaiseCoordinatesChanged();
            }
        }

        public int RAHours {
            get {
                return (int)Math.Truncate(_coordinates.RA);
            }
            set {
                if (value >= 0) {
                    _coordinates.RA = _coordinates.RA - RAHours + value;
                    RaiseCoordinatesChanged();
                }
            }
        }

        public int RAMinutes {
            get {
                return (int)(Math.Floor(_coordinates.RA * 60.0d) % 60);
            }
            set {
                if (value >= 0) {
                    _coordinates.RA = _coordinates.RA - RAMinutes / 60.0d + value / 60.0d;
                    RaiseCoordinatesChanged();
                }
            }
        }

        public int RASeconds {
            get {
                return (int)(Math.Floor(_coordinates.RA * 60.0d * 60.0d) % 60);
            }
            set {
                if (value >= 0) {
                    _coordinates.RA = _coordinates.RA - RASeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                    RaiseCoordinatesChanged();
                }
            }
        }

        public int DecDegrees {
            get {
                return (int)Math.Truncate(_coordinates.Dec);
            }
            set {
                if (value < 0) {
                    _coordinates.Dec = value - DecMinutes / 60.0d - DecSeconds / (60.0d * 60.0d);
                } else {
                    _coordinates.Dec = value + DecMinutes / 60.0d + DecSeconds / (60.0d * 60.0d);
                }
                RaiseCoordinatesChanged();
            }
        }

        public int DecMinutes {
            get {
                return (int)Math.Floor((Math.Abs(_coordinates.Dec * 60.0d) % 60));
            }
            set {
                if (_coordinates.Dec < 0) {
                    _coordinates.Dec = _coordinates.Dec + DecMinutes / 60.0d - value / 60.0d;
                } else {
                    _coordinates.Dec = _coordinates.Dec - DecMinutes / 60.0d + value / 60.0d;
                }

                RaiseCoordinatesChanged();
            }
        }

        public int DecSeconds {
            get {
                return (int)Math.Floor((Math.Abs(_coordinates.Dec * 60.0d * 60.0d) % 60));
            }
            set {
                if (_coordinates.Dec < 0) {
                    _coordinates.Dec = _coordinates.Dec + DecSeconds / (60.0d * 60.0d) - value / (60.0d * 60.0d);
                } else {
                    _coordinates.Dec = _coordinates.Dec - DecSeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                }

                RaiseCoordinatesChanged();
            }
        }

        private void RaiseCoordinatesChanged() {
            RaisePropertyChanged(nameof(Coordinates));
            RaisePropertyChanged(nameof(RAHours));
            RaisePropertyChanged(nameof(RAMinutes));
            RaisePropertyChanged(nameof(RASeconds));
            RaisePropertyChanged(nameof(DecDegrees));
            RaisePropertyChanged(nameof(DecMinutes));
            RaisePropertyChanged(nameof(DecSeconds));
            DSO = new DeepSkyObject(DSO?.Name ?? string.Empty, Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
        }

        private int _downloadProgressValue;

        public int DownloadProgressValue {
            get {
                return _downloadProgressValue;
            }
            set {
                _downloadProgressValue = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates _selectedCoordinates;

        public Coordinates SelectedCoordinates {
            get {
                return _selectedCoordinates;
            }
            set {
                _selectedCoordinates = value;
                RaisePropertyChanged();
            }
        }

        public double FieldOfView {
            get {
                return profileService.ActiveProfile.FramingAssistantSettings.FieldOfView;
            }
            set {
                profileService.ActiveProfile.FramingAssistantSettings.FieldOfView = value;
                RaisePropertyChanged();
            }
        }

        public int CameraWidth {
            get {
                return profileService.ActiveProfile.FramingAssistantSettings.CameraWidth;
            }
            set {
                profileService.ActiveProfile.FramingAssistantSettings.CameraWidth = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }

        public int CameraHeight {
            get {
                return profileService.ActiveProfile.FramingAssistantSettings.CameraHeight;
            }
            set {
                profileService.ActiveProfile.FramingAssistantSettings.CameraHeight = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }

        private SkySurveySource _framingAssistantSource;

        public SkySurveySource FramingAssistantSource {
            get {
                return _framingAssistantSource;
            }
            set {
                _framingAssistantSource = value;
                RaisePropertyChanged();
            }
        }

        private double _cameraPixelSize;

        public double CameraPixelSize {
            get {
                return _cameraPixelSize;
            }
            set {
                _cameraPixelSize = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }

        private int _focalLength;

        public int FocalLength {
            get {
                return _focalLength;
            }
            set {
                _focalLength = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }

        private SkySurveyImage _imageParameter;

        public SkySurveyImage ImageParameter {
            get {
                return _imageParameter;
            }
            set {
                _imageParameter = value;
                RaisePropertyChanged();
            }
        }

        private ObservableRectangle _rectangle;

        public ObservableRectangle Rectangle {
            get {
                return _rectangle;
            }
            set {
                _rectangle = value;
                RaisePropertyChanged();
            }
        }

        private IProgress<int> _progress;

        private CancellationTokenSource _loadImageSource;

        private IProgress<ApplicationStatus> _statusUpdate;

        private async Task<bool> LoadImage() {
            using (MyStopWatch.Measure()) {
                CancelLoadImage();
                _loadImageSource = new CancellationTokenSource();
                try {
                    var skySurvey = SkySurveyFactory.Create(FramingAssistantSource);

                    var skySurveyImage = await skySurvey.GetImage(DSO?.Name, this.Coordinates, Astrometry.DegreeToArcmin(FieldOfView), _loadImageSource.Token, _progress);

                    if (skySurveyImage != null) {
                        if (skySurveyImage.Coordinates == null) {
                            skySurveyImage = await PlateSolveSkySurvey(skySurveyImage);
                        }

                        CalculateRectangle(skySurveyImage);

                        await _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                            ImageParameter = null;
                            GC.Collect();
                            ImageParameter = skySurveyImage;
                        }));

                        SelectedImageCacheInfo = CacheSkySurvey.SaveImageToCache(skySurveyImage);
                        RaisePropertyChanged(nameof(ImageCacheInfo));
                    }
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
                return true;
            }
        }

        private async Task<SkySurveyImage> PlateSolveSkySurvey(SkySurveyImage skySurveyImage) {
            var diagResult = MyMessageBox.MyMessageBox.Show(string.Format(Locale.Loc.Instance["LblBlindSolveAttemptForFraming"], Coordinates.RAString, Coordinates.DecString), Locale.Loc.Instance["LblNoCoordinates"], MessageBoxButton.YesNo, MessageBoxResult.Yes);
            var solver = new PlatesolveVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
            PlateSolveResult psResult;
            if (diagResult == MessageBoxResult.Yes) {
                psResult = await solver.Solve(skySurveyImage.Image, _statusUpdate, _loadImageSource.Token, false, Coordinates);
            } else {
                psResult = await solver.BlindSolve(skySurveyImage.Image, _statusUpdate, _loadImageSource.Token);
            }

            if (psResult.Success) {
                var rotation = 180 - psResult.Orientation;
                if (rotation < 0) {
                    rotation += 360;
                } else if (rotation >= 360) {
                    rotation -= 360;
                }
                skySurveyImage.Coordinates = psResult.Coordinates;
                skySurveyImage.FoVWidth = Astrometry.ArcsecToArcmin(psResult.Pixscale * skySurveyImage.Image.Width);
                skySurveyImage.FoVHeight = Astrometry.ArcsecToArcmin(psResult.Pixscale * skySurveyImage.Image.Height);
                skySurveyImage.Rotation = rotation;
            } else {
                throw new Exception("Platesolve failed to retrieve coordinates for image");
            }
            return skySurveyImage;
        }

        public XElement ImageCacheInfo {
            get {
                return CacheSkySurvey.Cache; ;
            }
        }

        private XElement _selectedImageCacheInfo;

        public XElement SelectedImageCacheInfo {
            get {
                return _selectedImageCacheInfo;
            }
            set {
                _selectedImageCacheInfo = value;
                if (_selectedImageCacheInfo != null) {
                    var ra = double.Parse(_selectedImageCacheInfo.Attribute("RA").Value, CultureInfo.InvariantCulture);
                    var dec = double.Parse(_selectedImageCacheInfo.Attribute("Dec").Value, CultureInfo.InvariantCulture);
                    var name = _selectedImageCacheInfo.Attribute("Name").Value;
                    Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours);
                    FieldOfView = Astrometry.ArcminToDegree(double.Parse(_selectedImageCacheInfo.Attribute("FoVW").Value, CultureInfo.InvariantCulture));
                    DSO = new DeepSkyObject(name, Coordinates, string.Empty);
                }
                RaisePropertyChanged();
            }
        }

        private void CalculateRectangle(SkySurveyImage parameter) {
            if (parameter != null) {
                var imageArcsecWidth = Astrometry.ArcminToArcsec(parameter.FoVWidth) / parameter.Image.Width;
                var imageArcsecHeight = Astrometry.ArcminToArcsec(parameter.FoVHeight) / parameter.Image.Height;

                var arcsecPerPix = Astrometry.ArcsecPerPixel(CameraPixelSize, FocalLength);
                var conversion = arcsecPerPix / imageArcsecWidth;
                var width = CameraWidth * conversion;
                var height = CameraHeight * conversion;
                Rectangle = new ObservableRectangle(parameter.Rotation) { Width = width, Height = height, X = parameter.Image.Width / 2d - width / 2d, Y = parameter.Image.Height / 2d - height / 2d, Rotation = Rectangle?.Rotation ?? 0 };
                SelectedCoordinates = new Coordinates(parameter.Coordinates.RA, parameter.Coordinates.Dec, Epoch.J2000, Coordinates.RAType.Hours);
            }
        }

        private void DragStart(object obj) {
        }

        private void DragStop(object obj) {
        }

        private void DragMove(object obj) {
            var delta = ((DragResult)obj).Delta;
            this.Rectangle.X += delta.X;
            this.Rectangle.Y += delta.Y;

            var orientation = Astrometry.ToRadians(ImageParameter.Rotation);
            var x = delta.X * Math.Cos(orientation) + delta.Y * Math.Sin(orientation);
            var y = delta.Y * Math.Cos(orientation) - delta.X * Math.Sin(orientation);

            var imageArcsecWidth = Astrometry.ArcminToArcsec(ImageParameter.FoVWidth) / ImageParameter.Image.Width;
            var imageArcsecHeight = Astrometry.ArcminToArcsec(ImageParameter.FoVHeight) / ImageParameter.Image.Height;

            SelectedCoordinates = new Coordinates(
                SelectedCoordinates.RADegrees + Astrometry.ArcsecToDegree(x * imageArcsecWidth),
                SelectedCoordinates.Dec + Astrometry.ArcsecToDegree(y * imageArcsecHeight),
                Epoch.J2000,
                Coordinates.RAType.Degrees
            );
        }

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            if (cameraInfo != null) {
                if (this.CameraWidth != cameraInfo.XSize && cameraInfo.XSize > 0) {
                    this.CameraWidth = cameraInfo.XSize;
                }
                if (this.CameraHeight != cameraInfo.YSize && cameraInfo.YSize > 0) {
                    this.CameraHeight = cameraInfo.YSize;
                }
                if (this.CameraPixelSize != cameraInfo.PixelSize) {
                    CameraPixelSize = cameraInfo.PixelSize;
                }
            }
        }

        public ICommand DragStartCommand { get; private set; }
        public ICommand DragStopCommand { get; private set; }
        public ICommand DragMoveCommand { get; private set; }
        public IAsyncCommand LoadImageCommand { get; private set; }
        public ICommand CancelLoadImageCommand { get; private set; }
        public ICommand SetSequenceCoordinatesCommand { get; private set; }
        public IAsyncCommand SlewToCoordinatesCommand { get; private set; }
        public IAsyncCommand RecenterCommand { get; private set; }
        public ICommand CancelLoadImageFromFileCommand { get; private set; }
        public ICommand ClearCacheCommand { get; private set; }
    }
}