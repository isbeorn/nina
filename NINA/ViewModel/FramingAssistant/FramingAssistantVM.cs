#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyPlanetarium;
using NINA.PlateSolving;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Behaviors;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.Utility.SkySurvey;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using NINA.Model.ImageData;
using NINA.Utility.Exceptions;

namespace NINA.ViewModel.FramingAssistant {

    internal class FramingAssistantVM : BaseVM, ICameraConsumer {

        public FramingAssistantVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.telescopeMediator = telescopeMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            Opacity = 0.2;

            SkyMapAnnotator = new SkyMapAnnotator(telescopeMediator);

            var defaultCoordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);
            DSO = new DeepSkyObject(string.Empty, defaultCoordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);

            FramingAssistantSource = profileService.ActiveProfile.FramingAssistantSettings.LastSelectedImageSource;

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
            ClearCacheCommand = new RelayCommand(ClearCache, (object o) => Cache != null);
            RefreshSkyMapAnnotationCommand = new RelayCommand((object o) => SkyMapAnnotator.UpdateSkyMap(), (object o) => SkyMapAnnotator.Initialized);
            MouseWheelCommand = new RelayCommand(MouseWheel);

            CoordsFromPlanetariumCommand = new AsyncCommand<bool>(() => Task.Run(CoordsFromPlanetarium));

            DeepSkyObjectSearchVM = new DeepSkyObjectSearchVM();
            DeepSkyObjectSearchVM.PropertyChanged += DeepSkyObjectSearchVM_PropertyChanged;

            SetSequenceCoordinatesCommand = new AsyncCommand<bool>(async (object parameter) => {
                var vm = (ApplicationVM)Application.Current.Resources["AppVM"];
                vm.ChangeTab(ApplicationTab.SEQUENCE);

                var deepSkyObjects = new List<DeepSkyObject>();
                foreach (var rect in CameraRectangles) {
                    var name = rect.Id > 0 ? DSO?.Name + string.Format(" {0} ", Locale.Loc.Instance["LblPanel"]) + rect.Id : DSO?.Name;
                    var dso = new DeepSkyObject(name, rect.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                    dso.Rotation = Rectangle.DisplayedRotation;
                    deepSkyObjects.Add(dso);
                }

                bool msgResult = false;
                if (parameter.ToString() == "Replace") {
                    msgResult = await vm.SeqVM.SetSequenceCoordiantes(deepSkyObjects);
                } else if (parameter.ToString() == "Add") {
                    msgResult = await vm.SeqVM.SetSequenceCoordiantes(deepSkyObjects, false);
                }

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

            var appSettings = profileService.ActiveProfile.ApplicationSettings;
            appSettings.PropertyChanged += ApplicationSettings_PropertyChanged;

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                appSettings.PropertyChanged -= ApplicationSettings_PropertyChanged;
                RaisePropertyChanged(nameof(CameraPixelSize));
                RaisePropertyChanged(nameof(FocalLength));
                RaisePropertyChanged(nameof(FieldOfView));
                RaisePropertyChanged(nameof(CameraWidth));
                RaisePropertyChanged(nameof(CameraHeight));
                appSettings = profileService.ActiveProfile.ApplicationSettings;
                appSettings.PropertyChanged += ApplicationSettings_PropertyChanged;
                ApplicationSettings_PropertyChanged(null, null);
            };

            resizeTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, _dispatcher);
            resizeTimer.Interval = TimeSpan.FromMilliseconds(500);
            resizeTimer.Tick += ResizeTimer_Tick;

            ScrollViewerSizeChangedCommand = new RelayCommand((parameter) => {
                resizeTimer.Stop();
                if (ImageParameter != null && FramingAssistantSource == SkySurveySource.SKYATLAS) {
                    resizeTimer.Start();
                }
            });

            profileService.LocationChanged += (object sender, EventArgs e) => {
                DSO = new DeepSkyObject(DSO.Name, DSO.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            };

            InitializeCache();
        }

        private void InitializeCache() {
            try {
                Cache = new CacheSkySurvey(profileService.ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory);
                ImageCacheInfo = Cache.Cache;
                _selectedImageCacheInfo = (XElement)ImageCacheInfo?.FirstNode ?? null;
                RaisePropertyChanged(nameof(ImageCacheInfo));
            } catch (Exception ex) {
                Logger.Error(ex);
                Cache = null;
                ImageCacheInfo = null;
            }
            RaisePropertyChanged(nameof(ImageCacheInfo));
        }

        private void MouseWheel(object obj) {
            var delta = ((MouseWheelResult)obj).Delta;
            var stepSize = 2;
            if (delta > 0) {
                if (FieldOfView > 1) {
                    FieldOfView = Math.Max(1, FieldOfView - stepSize);
                }
            } else {
                if (FieldOfView < 200) {
                    FieldOfView = Math.Min(200, FieldOfView + stepSize);
                }
            }
            CalculateRectangle(SkyMapAnnotator.ChangeFoV(FieldOfView));
        }

        private async void ResizeTimer_Tick(object sender, EventArgs e) {
            using (MyStopWatch.Measure()) {
                (sender as DispatcherTimer).Stop();
                await LoadImage();
            }
        }

        private readonly DispatcherTimer resizeTimer;

        private void DeepSkyObjectSearchVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DeepSkyObjectSearchVM.Coordinates) && DeepSkyObjectSearchVM.Coordinates != null) {
                DSO = new DeepSkyObject(DeepSkyObjectSearchVM.SelectedTargetSearchResult.Column1, DeepSkyObjectSearchVM.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                RaiseCoordinatesChanged();
            }
        }

        public DeepSkyObjectSearchVM DeepSkyObjectSearchVM { get; private set; }

        private void ApplicationSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            InitializeCache();
        }

        private double opacity;

        public double Opacity {
            get => opacity;
            set {
                opacity = value;
                RaisePropertyChanged();
            }
        }

        private int fontSize;

        public int FontSize {
            get => fontSize;
            set {
                fontSize = value;
                RaisePropertyChanged();
            }
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
            if (Cache != null) {
                var diagResult = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblClearCache"] + "?", "", MessageBoxButton.YesNo, MessageBoxResult.No);
                if (diagResult == MessageBoxResult.Yes) {
                    Cache.Clear();
                    RaisePropertyChanged(nameof(ImageCacheInfo));
                }
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
            DeepSkyObjectSearchVM.SetTargetNameWithoutSearch(dso.Name);
            this.DSO = new DeepSkyObject(dso.Name, dso.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            FramingAssistantSource = profileService.ActiveProfile.FramingAssistantSettings.LastSelectedImageSource;
            if (FramingAssistantSource == SkySurveySource.CACHE || FramingAssistantSource == SkySurveySource.FILE) {
                FramingAssistantSource = SkySurveySource.NASA;
            }

            RaiseCoordinatesChanged();
            while (boundWidth == 0) {
                await Task.Delay(50);
            }
            await LoadImageCommand.ExecuteAsync(null);
            return true;
        }

        private void CancelLoadImage() {
            _loadImageSource?.Cancel();
        }

        private Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        private int boundWidth;

        public double BoundWidth {
            get => boundWidth;
            set => boundWidth = (int)value;
        }

        private int boundHeight;

        public double BoundHeight {
            get => boundHeight;
            set => boundHeight = (int)value;
        }

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
                CalculateRectangle(SkyMapAnnotator.ViewportFoV);
            }
        }

        public int CameraHeight {
            get {
                return profileService.ActiveProfile.FramingAssistantSettings.CameraHeight;
            }
            set {
                profileService.ActiveProfile.FramingAssistantSettings.CameraHeight = value;
                RaisePropertyChanged();
                CalculateRectangle(SkyMapAnnotator.ViewportFoV);
            }
        }

        private SkySurveySource _framingAssistantSource;

        public SkySurveySource FramingAssistantSource {
            get {
                return _framingAssistantSource;
            }
            set {
                _framingAssistantSource = value;
                if (profileService.ActiveProfile.FramingAssistantSettings.LastSelectedImageSource != value) {
                    profileService.ActiveProfile.FramingAssistantSettings.LastSelectedImageSource = _framingAssistantSource;
                }

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
                CalculateRectangle(SkyMapAnnotator.ViewportFoV);
            }
        }

        private AsyncObservableCollection<FramingRectangle> cameraRectangles;

        public AsyncObservableCollection<FramingRectangle> CameraRectangles {
            get {
                if (cameraRectangles == null) {
                    cameraRectangles = new AsyncObservableCollection<FramingRectangle>();
                }
                return cameraRectangles;
            }
            set {
                cameraRectangles = value;
                RaisePropertyChanged();
            }
        }

        private int horizontalPanels = 1;

        public int HorizontalPanels {
            get {
                return horizontalPanels;
            }
            set {
                horizontalPanels = value;
                RaisePropertyChanged();
                CalculateRectangle(SkyMapAnnotator.ViewportFoV);
            }
        }

        private int verticalPanels = 1;

        public int VerticalPanels {
            get {
                return verticalPanels;
            }
            set {
                verticalPanels = value;
                RaisePropertyChanged();
                CalculateRectangle(SkyMapAnnotator.ViewportFoV);
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
                CalculateRectangle(SkyMapAnnotator.ViewportFoV);
            }
        }

        private double rotation = 0;

        public double Rotation {
            get {
                return rotation;
            }
            set {
                var oldRotation = rotation;
                rotation = value;
                if (Rectangle != null && ImageParameter != null && rotation >= 0 && rotation <= 360) {
                    Rectangle.Rotation += (rotation - oldRotation) % 360;
                    if (Rectangle.Rotation < 0) { Rectangle.Rotation += 360; }
                    var center = new Point(Rectangle.X + Rectangle.Width / 2d, Rectangle.Y + Rectangle.Height / 2d);
                    var imageArcsecWidth = Astrometry.ArcminToArcsec(ImageParameter.FoVWidth) / ImageParameter.Image.Width;
                    var imageArcsecHeight = Astrometry.ArcminToArcsec(ImageParameter.FoVHeight) / ImageParameter.Image.Height;
                    foreach (var rect in CameraRectangles) {
                        var rectCenter = new Point(rect.X + Rectangle.X + rect.Width / 2d, rect.Y + Rectangle.Y + rect.Height / 2d);

                        var deltaX = rectCenter.X - center.X;
                        var deltaY = rectCenter.Y - center.Y;
                        rect.Coordinates = Rectangle.Coordinates.Shift(deltaX, deltaY, rotation, imageArcsecWidth, imageArcsecHeight);
                    }
                }
                RaisePropertyChanged();
            }
        }

        private double _focalLength;

        public double FocalLength {
            get {
                return _focalLength;
            }
            set {
                _focalLength = value;
                RaisePropertyChanged();
                CalculateRectangle(SkyMapAnnotator.ViewportFoV);
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
                _loadImageSource?.Dispose();
                _loadImageSource = new CancellationTokenSource();
                try {
                    SkySurveyImage skySurveyImage = null;

                    if (Cache != null && DSO != null) {
                        try {
                            skySurveyImage = await Cache.GetImage(FramingAssistantSource.GetCacheSourceString(), DSO.Coordinates.RA, DSO.Coordinates.Dec, DSO.Rotation, Astrometry.DegreeToArcmin(FieldOfView));
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                    }

                    if (skySurveyImage == null) {
                        if (FramingAssistantSource == SkySurveySource.CACHE) {
                            if (Cache == null) {
                                throw new Exception("Cache unavailable. Check log file for errors");
                            }
                            if (SelectedImageCacheInfo != null) {
                                skySurveyImage = await Cache.GetImage(Guid.Parse(SelectedImageCacheInfo.Attribute("Id").Value));
                            }
                        } else {
                            var skySurvey = SkySurveyFactory.Create(FramingAssistantSource);

                            skySurveyImage = await skySurvey.GetImage(DSO?.Name, DSO?.Coordinates,
                                Astrometry.DegreeToArcmin(FieldOfView), boundWidth, boundHeight, _loadImageSource.Token, _progress);
                        }
                    }

                    if (skySurveyImage != null) {
                        if (skySurveyImage.Coordinates == null) {
                            skySurveyImage = await PlateSolveSkySurvey(skySurveyImage);
                        }

                        await _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                            ImageParameter = null;
                            GC.Collect();
                            ImageParameter = skySurveyImage;
                        }));

                        if (Cache != null && FramingAssistantSource != SkySurveySource.SKYATLAS) {
                            SelectedImageCacheInfo = Cache.SaveImageToCache(skySurveyImage);
                            RaisePropertyChanged(nameof(ImageCacheInfo));
                        }

                        await SkyMapAnnotator.Initialize(skySurveyImage.Coordinates, Astrometry.ArcminToDegree(skySurveyImage.FoVHeight), ImageParameter.Image.PixelWidth, ImageParameter.Image.PixelHeight, ImageParameter.Rotation, _loadImageSource.Token);
                        SkyMapAnnotator.DynamicFoV = FramingAssistantSource == SkySurveySource.SKYATLAS;

                        CalculateRectangle(SkyMapAnnotator.ViewportFoV);
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

            var renderedImage = await RenderedImage.FromBitmapSource(source: skySurveyImage.Image);
            Coordinates coordinates = null;
            if (diagResult == MessageBoxResult.Yes) {
                coordinates = DSO.Coordinates;
            }
            var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
            var blindSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);

            var parameter = new PlateSolveParameter() {
                Binning = 1,
                Coordinates = coordinates,
                DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                FocalLength = this.FocalLength,
                MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                PixelSize = this.CameraPixelSize,
                Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
            };

            var imageSolver = new ImageSolver(plateSolver, blindSolver);
            var psResult = await imageSolver.Solve(renderedImage.RawImageData, parameter, _statusUpdate, _loadImageSource.Token);

            if (psResult?.Success == true) {
                var rotation = psResult.Orientation;
                if (rotation < 0) {
                    rotation += 360;
                } else if (rotation >= 360) {
                    rotation -= 360;
                }
                skySurveyImage.Coordinates = psResult.Coordinates;
                skySurveyImage.FoVWidth = Astrometry.ArcsecToArcmin(psResult.Pixscale * skySurveyImage.Image.PixelWidth);
                skySurveyImage.FoVHeight = Astrometry.ArcsecToArcmin(psResult.Pixscale * skySurveyImage.Image.PixelHeight);
                skySurveyImage.Rotation = rotation;
            } else {
                throw new Exception("Platesolve failed to retrieve coordinates for image");
            }

            return skySurveyImage;
        }

        public XElement ImageCacheInfo { get; set; }

        private CacheSkySurvey Cache { get; set; }

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

        private void CalculateRectangle(ViewportFoV parameter) {
            if (parameter != null) {
                var previousRotation = Rectangle?.Rotation ?? 0;
                Rectangle = null;
                Rotation = previousRotation;
                CameraRectangles.Clear();

                var centerCoordinates = parameter.CenterCoordinates;

                var imageArcsecWidth = Astrometry.DegreeToArcsec(parameter.OriginalHFoV) / parameter.OriginalWidth;
                var imageArcsecHeight = Astrometry.DegreeToArcsec(parameter.OriginalVFoV) / parameter.OriginalHeight;

                var arcsecPerPix = Astrometry.ArcsecPerPixel(CameraPixelSize, FocalLength);
                var conversion = arcsecPerPix / imageArcsecWidth;

                var width = CameraWidth * conversion;
                var height = CameraHeight * conversion;
                var x = parameter.OriginalWidth / 2d - width / 2d;
                var y = parameter.OriginalHeight / 2d - height / 2d;

                var cameraWidthArcSec = (CameraWidth) * arcsecPerPix;
                var cameraHeightArcSec = (CameraHeight) * arcsecPerPix;

                if (HorizontalPanels == 1 && VerticalPanels == 1) {
                    CameraRectangles.Add(new FramingRectangle(parameter.Rotation) {
                        Width = width,
                        Height = height,
                        X = 0,
                        Y = 0,
                        Rotation = previousRotation,
                        Coordinates = centerCoordinates
                    });
                } else {
                    var panelWidth = CameraWidth * conversion;
                    var panelHeight = CameraHeight * conversion;

                    var panelOverlapWidth = CameraWidth * OverlapPercentage * conversion;
                    var panelOverlapHeight = CameraHeight * OverlapPercentage * conversion;

                    cameraWidthArcSec = cameraWidthArcSec - (cameraWidthArcSec * OverlapPercentage);
                    cameraHeightArcSec = cameraHeightArcSec - (cameraHeightArcSec * OverlapPercentage);

                    width = HorizontalPanels * panelWidth - (HorizontalPanels - 1) * panelOverlapWidth;
                    height = VerticalPanels * panelHeight - (VerticalPanels - 1) * panelOverlapHeight;
                    x = parameter.OriginalWidth / 2d - width / 2d;
                    y = parameter.OriginalHeight / 2d - height / 2d;
                    var center = new Point(x + width / 2d, y + height / 2d);

                    var id = 1;
                    for (int i = 0; i < HorizontalPanels; i++) {
                        for (int j = 0; j < VerticalPanels; j++) {
                            var panelX = i * panelWidth - i * panelOverlapWidth;
                            var panelY = j * panelHeight - j * panelOverlapHeight;

                            var panelCenter = new Point(panelX + x + panelWidth / 2d, panelY + y + panelHeight / 2d);

                            var panelDeltaX = panelCenter.X - center.X;
                            var panelDeltaY = panelCenter.Y - center.Y;

                            var panelRotation = previousRotation;
                            var panelCenterCoordinates = centerCoordinates.Shift(panelDeltaX, panelDeltaY, panelRotation, imageArcsecWidth, imageArcsecHeight);
                            var rect = new FramingRectangle(parameter.Rotation) {
                                Id = id++,
                                Width = panelWidth,
                                Height = panelHeight,
                                X = panelX,
                                Y = panelY,
                                Rotation = panelRotation,
                                Coordinates = panelCenterCoordinates
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
                    Rotation = previousRotation,
                    Coordinates = centerCoordinates
                };

                FontSize = (int)((height / verticalPanels) * 0.1);
            }
        }

        private void DragStart(object obj) {
        }

        private void DragStop(object obj) {
        }

        private void DragMove(object obj) {
            var delta = ((DragResult)obj).Delta;
            if (FramingAssistantSource == SkySurveySource.SKYATLAS) {
                delta = new Vector(-delta.X, -delta.Y);

                var newCenter = SkyMapAnnotator.ShiftViewport(delta);
                DSO.Coordinates = newCenter;
                ImageParameter.Coordinates = newCenter;
                CalculateRectangle(SkyMapAnnotator.ViewportFoV);

                SkyMapAnnotator.UpdateSkyMap();
            } else {
                var imageArcsecWidth =
                    Astrometry.ArcminToArcsec(ImageParameter.FoVWidth) / ImageParameter.Image.Width;
                var imageArcsecHeight = Astrometry.ArcminToArcsec(ImageParameter.FoVHeight) /
                                        ImageParameter.Image.Height;
                this.Rectangle.X += delta.X;
                this.Rectangle.Y += delta.Y;

                Rectangle.Coordinates = Rectangle.Coordinates.Shift(delta.X, delta.Y, ImageParameter.Rotation,
                    imageArcsecWidth, imageArcsecHeight);
                foreach (var rect in CameraRectangles) {
                    rect.Coordinates = rect.Coordinates.Shift(delta.X, delta.Y, ImageParameter.Rotation,
                        imageArcsecWidth, imageArcsecHeight);
                }
            }
        }

        private bool prevCameraConnected = false;
        private SkyMapAnnotator skyMapAnnotator;

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

        private async Task<bool> CoordsFromPlanetarium() {
            IPlanetarium s = PlanetariumFactory.GetPlanetarium(profileService);
            DeepSkyObject resp = null;

            try {
                resp = await s.GetTarget();

                if (resp != null) {
                    await SetCoordinates(resp);
                    Notification.ShowSuccess(string.Format(Locale.Loc.Instance["LblPlanetariumCoordsOk"], s.Name));
                }
            } catch (PlanetariumObjectNotSelectedException) {
                Logger.Error($"Attempted to get coordinates from {s.Name} when no object was selected");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumObjectNotSelected"], s.Name));
            } catch (PlanetariumFailedToConnect ex) {
                Logger.Error($"Unable to connect to {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumFailedToConnect"], s.Name));
            } catch (Exception ex) {
                Logger.Error($"Failed to get coordinates from {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumCoordsError"], s.Name));
            }

            return (resp != null);
        }

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
        }

        public ICommand CoordsFromPlanetariumCommand { get; set; }
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
        public ICommand ScrollViewerSizeChangedCommand { get; }
        public ICommand RefreshSkyMapAnnotationCommand { get; }
        public ICommand MouseWheelCommand { get; }

        public SkyMapAnnotator SkyMapAnnotator {
            get => skyMapAnnotator;
            set {
                skyMapAnnotator = value;
                RaisePropertyChanged();
            }
        }
    }
}