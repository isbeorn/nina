#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.PlateSolving;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Behaviors;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

            Cache = new CacheSkySurvey(profileService.ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory);
            Opacity = 0.2;

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
            ClearCacheCommand = new RelayCommand(ClearCache);

            DeepSkyObjectSearchVM = new DeepSkyObjectSearchVM(profileService.ActiveProfile.ApplicationSettings.DatabaseLocation);
            DeepSkyObjectSearchVM.PropertyChanged += DeepSkyObjectSearchVM_PropertyChanged;

            FramingDec = new ObservableCollection<FramingDec>();

            SetSequenceCoordinatesCommand = new AsyncCommand<bool>(async (object parameter) => {
                var vm = (ApplicationVM)Application.Current.Resources["AppVM"];
                vm.ChangeTab(ApplicationTab.SEQUENCE);

                var deepSkyObjects = new List<DeepSkyObject>();
                foreach (var rect in CameraRectangles) {
                    var dso = new DeepSkyObject(DSO?.Name + string.Format(" {0} ", Locale.Loc.Instance["LblPanel"]) + rect.Id, rect.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
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

            _selectedImageCacheInfo = (XElement)ImageCacheInfo.FirstNode;

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

            DSOInImage = new AsyncObservableCollection<FramingDSO>();

            dbInstance = new DatabaseInteraction(profileService.ActiveProfile.ApplicationSettings.DatabaseLocation);
        }

        private async void ResizeTimer_Tick(object sender, EventArgs e) {
            using (MyStopWatch.Measure()) {
                (sender as DispatcherTimer).Stop();
                await LoadImage();
            }
        }

        private readonly DispatcherTimer resizeTimer;

        private DatabaseInteraction dbInstance;

        private void DeepSkyObjectSearchVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DeepSkyObjectSearchVM.Coordinates) && DeepSkyObjectSearchVM.Coordinates != null) {
                DSO = new DeepSkyObject(DeepSkyObjectSearchVM.SelectedTargetSearchResult.Column1, DeepSkyObjectSearchVM.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                RaiseCoordinatesChanged();
            }
        }

        public DeepSkyObjectSearchVM DeepSkyObjectSearchVM { get; private set; }

        private void ApplicationSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            Cache = new CacheSkySurvey(profileService.ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory);
            RaisePropertyChanged(nameof(ImageCacheInfo));
        }

        private double opacity;

        public double Opacity {
            get => opacity;
            set {
                opacity = value;
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
            var diagResult = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblClearCache"] + "?", "", MessageBoxButton.YesNo, MessageBoxResult.No);
            if (diagResult == MessageBoxResult.Yes) {
                Cache.Clear();
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
            DeepSkyObjectSearchVM.SetTargetNameWithoutSearch(dso.Name);
            this.DSO = new DeepSkyObject(dso.Name, dso.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            FramingAssistantSource = profileService.ActiveProfile.FramingAssistantSettings.LastSelectedImageSource;
            if (FramingAssistantSource == SkySurveySource.CACHE || FramingAssistantSource == SkySurveySource.FILE) {
                FramingAssistantSource = SkySurveySource.NASA;
            }

            RaiseCoordinatesChanged();
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

        private bool annotateDSO;

        public bool AnnotateDSO {
            get => annotateDSO;
            set {
                annotateDSO = value;
                RaisePropertyChanged();
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
                if (_framingAssistantSource == SkySurveySource.SKYATLAS) {
                    AnnotateDSO = true;
                }
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

        private int horizontalPanels = 1;

        public int HorizontalPanels {
            get {
                return horizontalPanels;
            }
            set {
                horizontalPanels = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
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

        private AsyncObservableCollection<FramingDSO> dSOInImage;

        public AsyncObservableCollection<FramingDSO> DSOInImage {
            get => dSOInImage;
            set {
                dSOInImage = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<FramingDec> framingDec;

        public ObservableCollection<FramingDec> FramingDec {
            get => framingDec;
            set {
                framingDec = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Query for skyobjects for a reference coordinate that overlap the current field of view
        /// </summary>
        /// <param name="referenceCoordinate"></param>
        /// <param name="fov"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, DeepSkyObject>> GetDeepSkyObjectsForFoV(Coordinates referenceCoordinate, double fov, CancellationToken ct) {
            var verticalFov = FieldOfView;
            var horizontalFov = ((double)boundWidth / boundHeight) * FieldOfView;

            var dsoDict = new Dictionary<string, DeepSkyObject>();
            DatabaseInteraction.DeepSkyObjectSearchParams param = new DatabaseInteraction.DeepSkyObjectSearchParams();

            Coordinates centerTop = referenceCoordinate.Shift(0, -verticalFov * 2 / 3, 0);
            Coordinates centerBottom = referenceCoordinate.Shift(0, verticalFov * 2 / 3, 0);

            // don't ask me what's left or right it might be either way, hell for all I know this might be reversed but it works atm
            Coordinates topOrBottomRight;
            Coordinates topOrBottomLeft;

            // if we are above 0 declination we want the upper bound of the fov since the ra fov at the top of the frame is wider than on the bottom
            // vice versa for the other way round
            if (referenceCoordinate.Dec >= 0) {
                topOrBottomRight = referenceCoordinate.Shift(-(horizontalFov) * 2 / 3, -(verticalFov) * 2 / 3, 0);
                topOrBottomLeft = referenceCoordinate.Shift(horizontalFov * 2 / 3, -(verticalFov) * 2 / 3, 0);
            } else {
                topOrBottomRight = referenceCoordinate.Shift(-(horizontalFov) * 2 / 3, (verticalFov) * 2 / 3, 0);
                topOrBottomLeft = referenceCoordinate.Shift(horizontalFov * 2 / 3, (verticalFov) * 2 / 3, 0);
            }

            // calculate the actually visible maximum horizontal fov
            var hFovDeg = topOrBottomLeft.RADegrees < topOrBottomRight.RADegrees
                ? topOrBottomRight.RADegrees - topOrBottomLeft.RADegrees
                : topOrBottomRight.RADegrees - topOrBottomLeft.RADegrees + 360;

            // if we're above 90deg centerTop will be different than centerBottom, otherwise it is equal
            // then we want everything from either centertop when it's below 0 or centerbottom if it's above 0
            // we also want a full circle of RA, hFovDeg also becomes 360 since the fov is a full circle
            if (Math.Abs(centerTop.RADegrees - centerBottom.RADegrees) > 0.0001) {
                if (referenceCoordinate.Dec < 0) {
                    param.Declination = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                        From = -90,
                        Thru = centerTop.Dec
                    };
                } else {
                    param.Declination = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                        From = centerBottom.Dec,
                        Thru = 90
                    };
                }

                hFovDeg = 360;

                param.RightAscension = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = 0,
                    Thru = 360
                };
            } else {
                param.Declination = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = centerBottom.Dec,
                    Thru = centerTop.Dec
                };
                param.RightAscension = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = topOrBottomLeft.RADegrees,
                    Thru = topOrBottomRight.RADegrees
                };
            }

            if (param.RightAscension.From > param.RightAscension.Thru) {
                param.RightAscension = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = topOrBottomLeft.RADegrees,
                    Thru = 360
                };

                foreach (var dso in await dbInstance.GetDeepSkyObjects(
                    string.Empty, param,
                    ct)) {
                    dsoDict.Add(dso.Id, dso);
                }

                param.RightAscension = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = 0,
                    Thru = topOrBottomRight.RADegrees
                };
            }

            foreach (var dso in await dbInstance.GetDeepSkyObjects(
                string.Empty, param,
                ct)) {
                if (!dsoDict.ContainsKey(dso.Id)) {
                    dsoDict.Add(dso.Id, dso);
                }
            }

            FramingDec.Clear();

            // TODO: something

            var step = 3.75;
            if (Math.Abs(referenceCoordinate.Dec) > 50) {
                step *= 2;
            }
            if (Math.Abs(referenceCoordinate.Dec) > 70) {
                step *= 2;
            }
            if (Math.Abs(referenceCoordinate.Dec) > 80) {
                step *= 2;
            }

            for (double i = topOrBottomLeft.RADegrees;
                i < topOrBottomLeft.RADegrees + (hFovDeg) + (hFovDeg == 360 ? 0 : step);
                i += step) {
                FramingDec.Add(new FramingDec(i > 0 ? i - i % step : i - (step + i % step), ImageParameter));
            }

            RAPathPoints = new ObservableCollection<PointCollectionAndClosed>();

            var groups = FramingDec.SelectMany(f => f.RAPoints).GroupBy(p => p.RA);

            foreach (var group in groups) {
                var pointCollection = new PointCollection(group.Select(f => new Point(f.X, f.Y)).ToArray());
                var closed = group.Count() == 360 / step;

                RAPathPoints.Add(new PointCollectionAndClosed() {Closed = closed, Collection = pointCollection});
            }

            return dsoDict;
        }

        public class PointCollectionAndClosed : BaseINPC {
            private PointCollection collection;
            private bool closed;

            public PointCollection Collection {
                get => collection;
                set {
                    collection = value;
                    RaisePropertyChanged();
                }
            }

            public bool Closed {
                get => closed;
                set {
                    closed = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ObservableCollection<PointCollectionAndClosed> raPathPoints;

        public ObservableCollection<PointCollectionAndClosed> RAPathPoints {
            get => raPathPoints;
            set {
                raPathPoints = value;
                RaisePropertyChanged();
            }
        }

        private Vector deltaSum = new Vector(0, 0);
        private Dictionary<string, DeepSkyObject> cachedDSOs = new Dictionary<string, DeepSkyObject>();

        private async Task UpdateDSOsInImage(Vector delta) {
            //deltaSum.X += delta.X;
            //deltaSum.Y += delta.Y;

            var imageArcsecWidth = Astrometry.ArcminToArcsec(ImageParameter.FoVWidth) / ImageParameter.Image.Width;
            var imageArcsecHeight = Astrometry.ArcminToArcsec(ImageParameter.FoVHeight) / ImageParameter.Image.Height;
            var newCenter = Rectangle.Coordinates.Shift(delta.X, delta.Y, ImageParameter.Rotation, imageArcsecWidth, imageArcsecHeight);
            DSO.Coordinates = newCenter;
            ImageParameter.Coordinates = newCenter;
            CalculateRectangle(ImageParameter);

            //if (Math.Abs(deltaSum.X) > ImageParameter.Image.Width / 4d || Math.Abs(deltaSum.Y) > ImageParameter.Image.Height / 4d) {
            cachedDSOs = await GetDeepSkyObjectsForFoV(newCenter, FieldOfView, _loadImageSource.Token);
            //deltaSum = new Vector(0, 0);
            //}

            var existingDSOs = new List<string>();
            for (int i = DSOInImage.Count - 1; i >= 0; i--) {
                var dso = DSOInImage[i];
                if (cachedDSOs.ContainsKey(dso.Id)) {
                    dso.RecalculateTopLeft(newCenter);
                    existingDSOs.Add(dso.Id);
                } else {
                    DSOInImage.RemoveAt(i);
                }
            }

            foreach (var framingDecItem in FramingDec) {
                framingDecItem.RecalculatePoints(newCenter);
            }

            var dsosToAdd = cachedDSOs.Where(x => !existingDSOs.Any(y => y == x.Value.Id));
            foreach (var dso in dsosToAdd) {
                DSOInImage.Add(new FramingDSO(dso.Value, ImageParameter));
            }
        }

        private async Task<bool> LoadImage() {
            using (MyStopWatch.Measure()) {
                CancelLoadImage();
                _loadImageSource = new CancellationTokenSource();
                try {
                    SkySurveyImage skySurveyImage;
                    if (FramingAssistantSource == SkySurveySource.CACHE) {
                        skySurveyImage = await Cache.GetImage(Guid.Parse(SelectedImageCacheInfo.Attribute("Id").Value));
                    } else {
                        var skySurvey = SkySurveyFactory.Create(FramingAssistantSource);

                        skySurveyImage = await skySurvey.GetImage(DSO?.Name, DSO?.Coordinates,
                            Astrometry.DegreeToArcmin(FieldOfView), boundWidth, boundHeight, _loadImageSource.Token, _progress);
                    }

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

                        if (FramingAssistantSource != SkySurveySource.SKYATLAS) {
                            SelectedImageCacheInfo = Cache.SaveImageToCache(skySurveyImage);
                            RaisePropertyChanged(nameof(ImageCacheInfo));
                        }

                        var l = new List<FramingDSO>();
                        cachedDSOs = await GetDeepSkyObjectsForFoV(skySurveyImage.Coordinates, FieldOfView, _loadImageSource.Token);
                        foreach (var dso in cachedDSOs) {
                            l.Add(new FramingDSO(dso.Value, skySurveyImage));
                        }
                        DSOInImage = new AsyncObservableCollection<FramingDSO>(l);

                        deltaSum = new Vector(0, 0);
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

        public XElement ImageCacheInfo => Cache.Cache;

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

        private void CalculateRectangle(SkySurveyImage parameter) {
            if (parameter != null) {
                Rectangle = null;
                Rotation = parameter.Rotation;
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

                if (HorizontalPanels == 1 && VerticalPanels == 1) {
                    CameraRectangles.Add(new FramingRectangle(parameter.Rotation) {
                        Width = width,
                        Height = height,
                        X = 0,
                        Y = 0,
                        Rotation = 0,
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
                    x = parameter.Image.Width / 2d - width / 2d;
                    y = parameter.Image.Height / 2d - height / 2d;
                    var center = new Point(x + width / 2d, y + height / 2d);

                    var id = 1;
                    for (int i = 0; i < HorizontalPanels; i++) {
                        for (int j = 0; j < VerticalPanels; j++) {
                            var panelX = i * panelWidth - i * panelOverlapWidth;
                            var panelY = j * panelHeight - j * panelOverlapHeight;

                            var panelCenter = new Point(panelX + x + panelWidth / 2d, panelY + y + panelHeight / 2d);

                            var panelDeltaX = panelCenter.X - center.X;
                            var panelDeltaY = panelCenter.Y - center.Y;

                            var panelRotation = parameter.Rotation;
                            var panelCenterCoordinates = centerCoordinates.Shift(panelDeltaX, panelDeltaY, panelRotation, imageArcsecWidth, imageArcsecHeight);
                            var rect = new FramingRectangle(parameter.Rotation) {
                                Id = id++,
                                Width = panelWidth,
                                Height = panelHeight,
                                X = panelX,
                                Y = panelY,
                                Rotation = 0,
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
                    Rotation = 0,
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
            if (FramingAssistantSource == SkySurveySource.SKYATLAS) {
                delta = new Vector(-delta.X, -delta.Y);
                UpdateDSOsInImage(delta).Wait();
            } else {
                var imageArcsecWidth = Astrometry.ArcminToArcsec(ImageParameter.FoVWidth) / ImageParameter.Image.Width;
                var imageArcsecHeight = Astrometry.ArcminToArcsec(ImageParameter.FoVHeight) / ImageParameter.Image.Height;
                this.Rectangle.X += delta.X;
                this.Rectangle.Y += delta.Y;

                Rectangle.Coordinates = Rectangle.Coordinates.Shift(delta.X, delta.Y, ImageParameter.Rotation, imageArcsecWidth, imageArcsecHeight);
                foreach (var rect in CameraRectangles) {
                    rect.Coordinates = rect.Coordinates.Shift(delta.X, delta.Y, ImageParameter.Rotation, imageArcsecWidth, imageArcsecHeight);
                }
            }
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
        public ICommand ScrollViewerSizeChangedCommand { get; }
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

    internal class FramingDSO : BaseINPC {
        private const int DSO_DEFAULT_SIZE = 30;

        private readonly double arcSecWidth;
        private readonly double arcSecHeight;
        private readonly double sizeWidth;
        private readonly double sizeHeight;
        private Point topLeftPoint;
        private readonly Point imageCenterPoint;

        /// <summary>
        /// Constructor for a Framing DSO.
        /// It takes a SkySurveyImage and a DeepSkyObject and calculates XY values in pixels from the top left edge of the image subtracting half of its size.
        /// Those coordinates can be used to place the DSO including its name and size in any given image.
        /// </summary>
        /// <param name="dso">The DSO including its coordinates</param>
        /// <param name="image">The image where the DSO should be placed in including the RA/Dec coordinates of the center of that image</param>
        public FramingDSO(DeepSkyObject dso, SkySurveyImage image) {
            arcSecWidth = Astrometry.ArcminToArcsec(image.FoVWidth) / image.Image.PixelWidth;
            arcSecHeight = Astrometry.ArcminToArcsec(image.FoVHeight) / image.Image.PixelHeight;

            if (dso.Size != null && dso.Size >= arcSecWidth) {
                sizeWidth = dso.Size.Value;
            } else {
                sizeWidth = DSO_DEFAULT_SIZE;
            }

            if (dso.Size != null && dso.Size >= arcSecHeight) {
                sizeHeight = dso.Size.Value;
            } else {
                sizeHeight = DSO_DEFAULT_SIZE;
            }

            Id = dso.Id;
            Name1 = dso.Name;
            Name2 = dso.AlsoKnownAs.FirstOrDefault(m => m.StartsWith("M "));
            Name3 = dso.AlsoKnownAs.FirstOrDefault(m => m.StartsWith("NGC "));

            if (Name3 != null && Name1 == Name3.Replace(" ", "")) {
                Name1 = null;
            }

            if (Name1 == null && Name2 == null) {
                Name1 = Name3;
                Name3 = null;
            }

            if (Name1 == null && Name2 != null) {
                Name1 = Name2;
                Name2 = Name3;
                Name3 = null;
            }

            //topLeftPoint = dso.Coordinates.ProjectFromCenterToXY(image.Coordinates, new Point(image.Image.PixelWidth / 2.0, image.Image.PixelHeight / 2.0),
            //  arcSecWidth, arcSecHeight, image.Rotation);

            imageCenterPoint = new Point(image.Image.PixelWidth / 2.0, image.Image.PixelHeight / 2.0);
            rotation = image.Rotation;
            coordinates = dso.Coordinates;

            RecalculateTopLeft(image.Coordinates);
        }

        private Coordinates coordinates;
        private double rotation;

        public void RecalculateTopLeft(Coordinates reference) {
            var projectedPoint = coordinates.ProjectFromCenterToXY(
                reference,
                imageCenterPoint,
                arcSecWidth,
                arcSecHeight,
                rotation
            );
            TopLeftPoint = new Point(projectedPoint.X - SizeWidth / 2, projectedPoint.Y - SizeHeight / 2);
        }

        public double SizeWidth => sizeWidth / arcSecWidth;

        public double SizeHeight => sizeHeight / arcSecHeight;

        public Point TopLeftPoint {
            get => topLeftPoint;
            private set {
                topLeftPoint = value;
                RaisePropertyChanged();
            }
        }

        public string Id { get; }
        public string Name1 { get; }
        public string Name2 { get; }
        public string Name3 { get; }
    }

    internal class FramingDec : BaseINPC {
        private readonly double arcSecWidth;
        private readonly double arcSecHeight;
        private Point topPoint;
        private Point bottomPoint;
        private readonly Point imageCenterPoint;

        /// <summary>
        /// Constructor for a Framing DSO.
        /// It takes a SkySurveyImage and a DeepSkyObject and calculates XY values in pixels from the top left edge of the image subtracting half of its size.
        /// Those coordinates can be used to place the DSO including its name and size in any given image.
        /// </summary>
        /// <param name="dso">The DSO including its coordinates</param>
        /// <param name="image">The image where the DSO should be placed in including the RA/Dec coordinates of the center of that image</param>
        public FramingDec(double angle, SkySurveyImage image) {
            var dec = image.Coordinates.Dec;

            var fovHeight = Astrometry.ArcminToDegree(image.FoVHeight);

            arcSecWidth = Astrometry.ArcminToArcsec(image.FoVWidth) / image.Image.PixelWidth;
            arcSecHeight = Astrometry.ArcminToArcsec(image.FoVHeight) / image.Image.PixelHeight;

            imageCenterPoint = new Point(image.Image.PixelWidth / 2.0, image.Image.PixelHeight / 2.0);
            rotation = image.Rotation;

            // calculate the lines based on fov height and current dec to avoid projection issues
            // atan gnomoric projection cannot project properly over 90deg, it will result in the same results as prior
            // and dec lines will overlap each other
            var bottomDec = dec >= 0 ? dec - fovHeight /2 : dec + fovHeight /2;
            var topDec = Math.Abs(dec) + fovHeight > 89 ? (dec > 0 ? 89 : -89) : ((dec >= 0) ? dec + fovHeight/2 : dec - fovHeight/2);

            raCoordinates = new List<Coordinates>();
            raPoints = new ObservableCollection<RAPoint>();

            for (int i = (int) bottomDec;
                i <= (int)topDec;
                i++) {
                raCoordinates.Add(new Coordinates(angle, i, Epoch.J2000, Coordinates.RAType.Degrees));
            }

            coordinatesTop = new Coordinates(angle, topDec, Epoch.J2000, Coordinates.RAType.Degrees);
            coordinatesBottom = new Coordinates(angle, bottomDec, Epoch.J2000, Coordinates.RAType.Degrees);

            RecalculatePoints(image.Coordinates);
        }

        private readonly Coordinates coordinatesTop;
        private readonly double rotation;
        private readonly Coordinates coordinatesBottom;
        private List<Coordinates> raCoordinates;
        private ObservableCollection<RAPoint> raPoints;

        public void RecalculatePoints(Coordinates reference) {
            TopPoint = coordinatesTop.ProjectFromCenterToXY(
                reference,
                imageCenterPoint,
                arcSecWidth,
                arcSecHeight,
                rotation
            );

            BottomPoint = coordinatesBottom.ProjectFromCenterToXY(
                reference,
                imageCenterPoint,
                arcSecWidth,
                arcSecHeight,
                rotation
            );

            foreach (var coord in raCoordinates) {
                var point = coord.ProjectFromCenterToXY(
                    reference,
                    imageCenterPoint,
                    arcSecWidth,
                    arcSecHeight,
                    rotation
                );
                raPoints.Add(new RAPoint() { X = point.X, Y = point.Y, RA = coord.Dec});
            }
        }

        public struct RAPoint {
            public double X;
            public double Y;
            public double RA;
        }

        public ObservableCollection<RAPoint> RAPoints {
            get => raPoints;
            private set {
                raPoints = value;
                RaisePropertyChanged();
            }
        }

        public Point TopPoint {
            get => topPoint;
            private set {
                topPoint = value;
                RaisePropertyChanged();
            }
        }

        public Point BottomPoint {
            get => bottomPoint;
            private set {
                bottomPoint = value;
                RaisePropertyChanged();
            }
        }
    }
}