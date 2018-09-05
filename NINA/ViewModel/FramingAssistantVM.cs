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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Linq;
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

            var defaultCoordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);
            DSO = new DeepSkyObject(string.Empty, defaultCoordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);

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

                var deepSkyObjects = new List<DeepSkyObject>();
                foreach (var rect in CameraRectangles) {
                    var dso = new DeepSkyObject(DSO?.Name + " Panel " + rect.Id, rect.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                    dso.Rotation = Rectangle.DisplayedRotation;
                    deepSkyObjects.Add(dso);
                }

                //var dso = new DeepSkyObject(DSO?.Name, Rectangle.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                //dso.Rotation = Rectangle.DisplayedRotation;
                var msgResult = await vm.SeqVM.SetSequenceCoordiantes(deepSkyObjects);

                ImageParameter = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return msgResult;
            }, (object o) => Rectangle?.Coordinates != null);

            RecenterCommand = new AsyncCommand<bool>(async () => {
                DSO.Coordinates = Rectangle.Coordinates;
                await LoadImageCommand.ExecuteAsync(null);
                return true;
            }, (object o) => Rectangle?.Coordinates != null);

            SlewToCoordinatesCommand = new AsyncCommand<bool>(async () => {
                return await telescopeMediator.SlewToCoordinatesAsync(Rectangle.Coordinates);
            }, (object o) => Rectangle?.Coordinates != null);

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

        private ICameraMediator cameraMediator;
        private ITelescopeMediator telescopeMediator;
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        public int RAHours {
            get {
                return (int)Math.Truncate(DSO.Coordinates.RA);
            }
            set {
                if (value >= 0) {
                    DSO.Coordinates.RA = DSO.Coordinates.RA - RAHours + value;
                    RaiseCoordinatesChanged();
                }
            }
        }

        public int RAMinutes {
            get {
                return (int)(Math.Floor(DSO.Coordinates.RA * 60.0d) % 60);
            }
            set {
                if (value >= 0) {
                    DSO.Coordinates.RA = DSO.Coordinates.RA - RAMinutes / 60.0d + value / 60.0d;
                    RaiseCoordinatesChanged();
                }
            }
        }

        public int RASeconds {
            get {
                return (int)(Math.Floor(DSO.Coordinates.RA * 60.0d * 60.0d) % 60);
            }
            set {
                if (value >= 0) {
                    DSO.Coordinates.RA = DSO.Coordinates.RA - RASeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                    RaiseCoordinatesChanged();
                }
            }
        }

        public int DecDegrees {
            get {
                return (int)Math.Truncate(DSO.Coordinates.Dec);
            }
            set {
                if (value < 0) {
                    DSO.Coordinates.Dec = value - DecMinutes / 60.0d - DecSeconds / (60.0d * 60.0d);
                } else {
                    DSO.Coordinates.Dec = value + DecMinutes / 60.0d + DecSeconds / (60.0d * 60.0d);
                }
                RaiseCoordinatesChanged();
            }
        }

        public int DecMinutes {
            get {
                return (int)Math.Floor((Math.Abs(DSO.Coordinates.Dec * 60.0d) % 60));
            }
            set {
                if (DSO.Coordinates.Dec < 0) {
                    DSO.Coordinates.Dec = DSO.Coordinates.Dec + DecMinutes / 60.0d - value / 60.0d;
                } else {
                    DSO.Coordinates.Dec = DSO.Coordinates.Dec - DecMinutes / 60.0d + value / 60.0d;
                }

                RaiseCoordinatesChanged();
            }
        }

        public int DecSeconds {
            get {
                return (int)Math.Floor((Math.Abs(DSO.Coordinates.Dec * 60.0d * 60.0d) % 60));
            }
            set {
                if (DSO.Coordinates.Dec < 0) {
                    DSO.Coordinates.Dec = DSO.Coordinates.Dec + DecSeconds / (60.0d * 60.0d) - value / (60.0d * 60.0d);
                } else {
                    DSO.Coordinates.Dec = DSO.Coordinates.Dec - DecSeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                }

                RaiseCoordinatesChanged();
            }
        }

        private void RaiseCoordinatesChanged() {
            RaisePropertyChanged(nameof(RAHours));
            RaisePropertyChanged(nameof(RAMinutes));
            RaisePropertyChanged(nameof(RASeconds));
            RaisePropertyChanged(nameof(DecDegrees));
            RaisePropertyChanged(nameof(DecMinutes));
            RaisePropertyChanged(nameof(DecSeconds));
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

        private ObservableCollection<FramingRectangle> cameraRectangles;

        public ObservableCollection<FramingRectangle> CameraRectangles {
            get {
                if (cameraRectangles == null) {
                    cameraRectangles = new ObservableCollection<FramingRectangle>();
                }
                return cameraRectangles;
            }
            set {
                cameraRectangles = value;
                RaisePropertyChanged();
            }
        }

        private double targetWidthDegrees = 0.5;

        public double TargetWidthDegrees {
            get {
                return targetWidthDegrees;
            }
            set {
                targetWidthDegrees = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }

        private double targetHeightDegrees = 1.5;

        public double TargetHeightDegrees {
            get {
                return targetHeightDegrees;
            }
            set {
                targetHeightDegrees = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }

        private double overlapPercentage = 0.2;

        public double OverlapPercentage {
            get {
                return overlapPercentage;
            }
            set {
                overlapPercentage = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }

        private double rotation = 0;

        public double Rotation {
            get {
                return rotation;
            }
            set {
                rotation = value;
                if (Rectangle != null && ImageParameter != null && rotation >= 0 && rotation <= 360) {
                    Rectangle.Rotation = value;
                    var center = new Point(Rectangle.X + Rectangle.Width / 2d, Rectangle.Y + Rectangle.Height / 2d);
                    foreach (var rect in CameraRectangles) {
                        var rectCenter = new Point(rect.X + Rectangle.X + rect.Width / 2d, rect.Y + Rectangle.Y + rect.Height / 2d);

                        var deltaX = -(rectCenter.X - center.X);
                        var deltaY = rectCenter.Y - center.Y;
                        var imageArcsecWidth = Astrometry.ArcminToArcsec(ImageParameter.FoVWidth) / ImageParameter.Image.Width;
                        var imageArcsecHeight = Astrometry.ArcminToArcsec(ImageParameter.FoVHeight) / ImageParameter.Image.Height;
                        rect.Coordinates = ShiftCoordinates(Rectangle.Coordinates, deltaX, deltaY, (180 + rotation) % 360, imageArcsecWidth, imageArcsecHeight);
                    }
                }
                RaisePropertyChanged();
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

        private FramingRectangle _rectangle;

        public FramingRectangle Rectangle {
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

                    var skySurveyImage = await skySurvey.GetImage(DSO?.Name, DSO?.Coordinates, Astrometry.DegreeToArcmin(FieldOfView), _loadImageSource.Token, _progress);

                    if (skySurveyImage != null) {
                        if (skySurveyImage.Coordinates == null) {
                            skySurveyImage = await PlateSolveSkySurvey(skySurveyImage);
                        }

                        CalculateRectangle(skySurveyImage);

                        await _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                            ImageParameter = null;
                            GC.Collect();
                            ImageParameter = skySurveyImage;
                            Rotation = ImageParameter.Rotation;
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
            var diagResult = MyMessageBox.MyMessageBox.Show(string.Format(Locale.Loc.Instance["LblBlindSolveAttemptForFraming"], DSO.Coordinates.RAString, DSO.Coordinates.DecString), Locale.Loc.Instance["LblNoCoordinates"], MessageBoxButton.YesNo, MessageBoxResult.Yes);
            var solver = new PlatesolveVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
            PlateSolveResult psResult;
            if (diagResult == MessageBoxResult.Yes) {
                psResult = await solver.Solve(skySurveyImage.Image, _statusUpdate, _loadImageSource.Token, false, DSO.Coordinates);
            } else {
                psResult = await solver.BlindSolve(skySurveyImage.Image, _statusUpdate, _loadImageSource.Token);
            }

            if (psResult.Success) {
                var rotation = psResult.Orientation;
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
                    var coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours);
                    FieldOfView = Astrometry.ArcminToDegree(double.Parse(_selectedImageCacheInfo.Attribute("FoVW").Value, CultureInfo.InvariantCulture));
                    DSO = new DeepSkyObject(name, coordinates, string.Empty);
                    RaiseCoordinatesChanged();
                }
                RaisePropertyChanged();
            }
        }

        private void CalculateRectangle(SkySurveyImage parameter) {
            if (parameter != null) {
                CameraRectangles.Clear();

                var centerCoordinates = new Coordinates(parameter.Coordinates.RA, parameter.Coordinates.Dec, Epoch.J2000, Coordinates.RAType.Hours);

                var imageArcsecWidth = Astrometry.ArcminToArcsec(parameter.FoVWidth) / parameter.Image.Width;
                var imageArcsecHeight = Astrometry.ArcminToArcsec(parameter.FoVHeight) / parameter.Image.Height;

                var arcsecPerPix = Astrometry.ArcsecPerPixel(CameraPixelSize, FocalLength);
                var conversion = arcsecPerPix / imageArcsecWidth;

                var width = CameraWidth * conversion;
                var height = CameraHeight * conversion;
                var x = parameter.Image.Width / 2d - width / 2d;
                var y = parameter.Image.Height / 2d - height / 2d;

                var cameraWidthArcSec = (CameraWidth) * arcsecPerPix;
                var cameraHeightArcSec = (CameraHeight) * arcsecPerPix;

                var targetWidthArcSec = Astrometry.DegreeToArcsec(TargetWidthDegrees);
                var targetHeightArcSec = Astrometry.DegreeToArcsec(TargetHeightDegrees);

                if (cameraWidthArcSec > targetWidthArcSec && cameraHeightArcSec > targetHeightArcSec) {
                    CameraRectangles.Add(new FramingRectangle(parameter.Rotation) {
                        Width = width,
                        Height = height,
                        X = 0,
                        Y = 0,
                        Rotation = Rectangle?.Rotation ?? 0,
                        Coordinates = centerCoordinates
                    });
                } else {
                    var panelWidth = CameraWidth * conversion;
                    var panelHeight = CameraHeight * conversion;

                    var panelOverlapWidth = CameraWidth * OverlapPercentage * conversion;
                    var panelOverlapHeight = CameraHeight * OverlapPercentage * conversion;

                    cameraWidthArcSec = cameraWidthArcSec - (cameraWidthArcSec * OverlapPercentage);
                    cameraHeightArcSec = cameraHeightArcSec - (cameraHeightArcSec * OverlapPercentage);

                    // multi panel
                    var horizontalPanels = Math.Ceiling(targetWidthArcSec / cameraWidthArcSec);
                    var verticalPanels = Math.Ceiling(targetHeightArcSec / cameraHeightArcSec);

                    width = horizontalPanels * panelWidth - (horizontalPanels - 1) * panelOverlapWidth;
                    height = verticalPanels * panelHeight - (verticalPanels - 1) * panelOverlapHeight;
                    x = parameter.Image.Width / 2d - width / 2d;
                    y = parameter.Image.Height / 2d - height / 2d;
                    var center = new Point(x + width / 2d, y + height / 2d);

                    var id = 1;
                    for (int i = 0; i < horizontalPanels; i++) {
                        for (int j = 0; j < verticalPanels; j++) {
                            var panelX = i * panelWidth - i * panelOverlapWidth;
                            var panelY = j * panelHeight - j * panelOverlapHeight;

                            var panelCenter = new Point(panelX + x + panelWidth / 2d, panelY + y + panelHeight / 2d);

                            var panelDeltaX = panelCenter.X - center.X;
                            var panelDeltaY = panelCenter.Y - center.Y;

                            var panelRotation = parameter.Rotation;
                            var panelCenterCoodrinates = ShiftCoordinates(centerCoordinates, panelDeltaX, panelDeltaY, panelRotation, imageArcsecWidth, imageArcsecHeight);
                            var rect = new FramingRectangle(parameter.Rotation) {
                                Id = id++,
                                Width = panelWidth,
                                Height = panelHeight,
                                X = panelX,
                                Y = panelY,
                                Rotation = Rectangle?.Rotation ?? 0,
                                Coordinates = panelCenterCoodrinates
                            };
                            CameraRectangles.Add(rect);
                        }
                    }
                }

                Rectangle = new FramingRectangle(parameter.Rotation) {
                    Width = width,
                    Height = height,
                    X = x,
                    Y = y,
                    Rotation = Rectangle?.Rotation ?? 0,
                    Coordinates = centerCoordinates
                };
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

            var imageArcsecWidth = Astrometry.ArcminToArcsec(ImageParameter.FoVWidth) / ImageParameter.Image.Width;
            var imageArcsecHeight = Astrometry.ArcminToArcsec(ImageParameter.FoVHeight) / ImageParameter.Image.Height;
            Rectangle.Coordinates = ShiftCoordinates(Rectangle.Coordinates, delta.X, delta.Y, ImageParameter.Rotation, imageArcsecWidth, imageArcsecHeight);

            foreach (var rect in CameraRectangles) {
                rect.Coordinates = ShiftCoordinates(rect.Coordinates, delta.X, delta.Y, ImageParameter.Rotation, imageArcsecWidth, imageArcsecHeight);
            }
        }

        private Point RotatePoint(Point center, Point origin, double angle) {
            var point = new Point(origin.X, origin.Y);
            var s = Math.Sin(Astrometry.ToRadians(angle));
            var c = Math.Cos(Astrometry.ToRadians(angle));

            point.X -= center.X;
            point.Y -= center.Y;

            double xNew = point.X * c - point.Y * s;
            double yNew = point.X * s + point.Y * c;

            point.X = xNew + center.X;
            point.Y = yNew + center.Y;

            return point;
        }

        /// <summary>
        /// Shift coordinates by a delta
        /// </summary>
        /// <param name="origin">Coordinates to shift from</param>
        /// <param name="deltaX">delta x</param>
        /// <param name="deltaY">delta y</param>
        /// <param name="rotation">rotation relative to delta values</param>
        /// <param name="scaleX">scale relative to deltaX in arcsecs</param>
        /// <param name="scaleY">scale raltive to deltaY in arcsecs</param>
        /// <returns></returns>
        private Coordinates ShiftCoordinates(
                Coordinates origin,
                double deltaX,
                double deltaY,
                double rotation,
                double scaleX,
                double scaleY
        ) {
            var orientation = Astrometry.ToRadians(rotation);
            var x = deltaX * Math.Cos(orientation) + deltaY * Math.Sin(orientation);
            var y = deltaY * Math.Cos(orientation) + deltaX * Math.Sin(orientation);

            return new Coordinates(
                origin.RADegrees + Astrometry.ArcsecToDegree(x * scaleX),
                origin.Dec + Astrometry.ArcsecToDegree(y * scaleY),
                Epoch.J2000,
                Coordinates.RAType.Degrees
            );
        }

        private bool prevCameraConnected = false;

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            if (cameraInfo != null) {
                if (cameraInfo.Connected == true && prevCameraConnected == false) {
                    if (this.CameraWidth != cameraInfo.XSize && cameraInfo.XSize > 0) {
                        this.CameraWidth = cameraInfo.XSize;
                    }
                    if (this.CameraHeight != cameraInfo.YSize && cameraInfo.YSize > 0) {
                        this.CameraHeight = cameraInfo.YSize;
                    }
                    if (this.CameraPixelSize != cameraInfo.PixelSize && cameraInfo.PixelSize > 0) {
                        CameraPixelSize = cameraInfo.PixelSize;
                    }
                }
                prevCameraConnected = cameraInfo.Connected;
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

    internal class FramingRectangle : ObservableRectangle {

        public FramingRectangle(double rotationOffset) : base(rotationOffset) {
        }

        public FramingRectangle(double x, double y, double width, double height) : base(x, y, width, height) {
        }

        private int id;

        public int Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates coordinates;

        public Coordinates Coordinates {
            get {
                return coordinates;
            }
            set {
                coordinates = value;
                RaisePropertyChanged();
            }
        }
    }
}