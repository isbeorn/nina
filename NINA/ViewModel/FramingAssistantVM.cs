using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace NINA.ViewModel {
    class FramingAssistantVM : BaseVM {

        public FramingAssistantVM() {
            Coordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);
            DSO = new DeepSkyObject(string.Empty, Coordinates);
            //Coordinates = new Coordinates(073.2920, -07.6335, Epoch.J2000, Coordinates.RAType.Degrees);
            //Coordinates = new Coordinates(10.6833, 41.2686, Epoch.J2000, Coordinates.RAType.Degrees);

            CameraPixelSize = ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize;
            FocalLength = ProfileManager.Instance.ActiveProfile.TelescopeSettings.FocalLength;

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
                var msgResult = await Mediator.Instance.RequestAsync(new SetSequenceCoordinatesMessage() { DSO = new DeepSkyObject(DSO?.Name, SelectedCoordinates) });
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
                    return await Mediator.Instance.RequestAsync(new SlewToCoordinatesMessage() { Coordinates = SelectedCoordinates });
                }
                return false;
            }, (object o) => SelectedCoordinates != null);


            RegisterMediatorMessages();
            LoadImageCacheList();

        }

        private void ClearCache(object obj) {
            var diagResult = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblClearCache"] + "?", "", MessageBoxButton.YesNo, MessageBoxResult.No);
            if (diagResult == MessageBoxResult.Yes) {
                System.IO.DirectoryInfo di = new DirectoryInfo(FRAMINGASSISTANTCACHEPATH);

                foreach (FileInfo file in di.GetFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories()) {
                    dir.Delete(true);
                }

                LoadImageCacheList();
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

                Mediator.Instance.Request(new StatusUpdateMessage() { Status = _status });
            }
        }

        private async Task<bool> LoadImageFromFile() {

            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Locale.Loc.Instance["LblLoadImage"];
            dialog.FileName = "";
            dialog.DefaultExt = ".tif";
            dialog.Multiselect = false;
            dialog.Filter = "Image files|*.tif;*.tiff;*.jpeg;*.jpg;*.png|TIFF files|*.tif;*.tiff;|JPEG files|*.jpeg;*.jpg|PNG Files|*.png";

            if (dialog.ShowDialog() == true) {

                BitmapSource img = null;
                switch (Path.GetExtension(dialog.FileName)) {
                    case ".tif":
                    case ".tiff":
                        img = LoadTiff(dialog.FileName);
                        break;
                    case ".png":
                        img = LoadPng(dialog.FileName);
                        break;
                    case ".jpg":
                        img = LoadJpg(dialog.FileName);
                        break;
                }

                if (img == null) {
                    return false;
                }

                var dialogResult = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblBlindSolveAttemptForFraming"], Locale.Loc.Instance["LblNoCoordinates"], MessageBoxButton.OKCancel, MessageBoxResult.OK);
                if (dialogResult == MessageBoxResult.OK) {
                    var plateSolveResult = await Mediator.Instance.RequestAsync(new PlateSolveMessage() { Image = img, Progress = _statusUpdate, Token = _loadImageSource.Token, Blind = true });
                    if (plateSolveResult.Success) {
                        var rotation = 180 - plateSolveResult.Orientation;
                        if (rotation < 0) {
                            rotation += 360;
                        } else if (rotation >= 360) {
                            rotation -= 360;
                        }

                        var parameter = new FramingImageParameter() {
                            Image = img,
                            FieldOfViewWidth = Astrometry.ArcsecToDegree(plateSolveResult.Pixscale * img.Width),
                            FieldOfViewHeight = Astrometry.ArcsecToDegree(plateSolveResult.Pixscale * img.Height),
                            Rotation = rotation
                        };
                        Coordinates = plateSolveResult.Coordinates;
                        DSO.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
                        FieldOfView = Math.Round(Math.Max(parameter.FieldOfViewWidth, parameter.FieldOfViewHeight), 2);
                        CalculateRectangle(parameter);
                        await _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                            ImageParameter = parameter;
                        }));

                        return true;
                    } else {
                        return false;
                    }

                } else {
                    return false;
                }


            } else {
                return false;
            }
        }

        private BitmapSource LoadPng(string filename) {
            PngBitmapDecoder PngDec = new PngBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return PngDec.Frames[0];
        }

        private BitmapSource LoadJpg(string filename) {
            JpegBitmapDecoder JpgDec = new JpegBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return JpgDec.Frames[0];
        }

        private BitmapSource LoadTiff(string filename) {
            TiffBitmapDecoder TifDec = new TiffBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return TifDec.Frames[0];
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterAsyncRequest(new SetFramingAssistantCoordinatesMessageHandle(async (SetFramingAssistantCoordinatesMessage m) => {
                Mediator.Instance.Request(new ChangeApplicationTabMessage() { Tab = ApplicationTab.FRAMINGASSISTANT });
                this.DSO = new DeepSkyObject(m.DSO.Name, m.DSO.Coordinates);
                this.Coordinates = m.DSO.Coordinates;
                FramingAssistantSource = FramingAssistantSource.DSS;
                await LoadImageCommand.ExecuteAsync(null);
                return true;
            }));

            Mediator.Instance.Register((object o) => {
                var cam = (ICamera)o;
                this.CameraWidth = cam?.CameraXSize ?? this.CameraWidth;
                this.CameraHeight = cam?.CameraYSize ?? this.CameraHeight;
            }, MediatorMessages.CameraChanged);

            Mediator.Instance.Register((object o) => {
                DSO = new DeepSkyObject(DSO.Name, DSO.Coordinates);
            }, MediatorMessages.LocationChanged);


            Mediator.Instance.Register((object o) => {
                CameraPixelSize = (double)o;
            }, MediatorMessages.CameraPixelSizeChanged);
        }

        private void CancelLoadImage() {
            _loadImageSource?.Cancel();
        }

        Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        DeepSkyObject _dSO;
        public DeepSkyObject DSO {
            get {
                return _dSO;
            }
            set {
                _dSO = value;
                _dSO?.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), ProfileManager.Instance.ActiveProfile.AstrometrySettings.Latitude, ProfileManager.Instance.ActiveProfile.AstrometrySettings.Longitude);
                RaisePropertyChanged();
            }
        }

        Coordinates _coordinates;
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
            DSO = new DeepSkyObject(DSO?.Name ?? string.Empty, Coordinates);
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

        Coordinates _selectedCoordinates;
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
                return ProfileManager.Instance.ActiveProfile.FramingAssistantSettings.FieldOfView;
            }
            set {
                ProfileManager.Instance.ActiveProfile.FramingAssistantSettings.FieldOfView = value;
                RaisePropertyChanged();
            }
        }

        public int CameraWidth {
            get {
                return ProfileManager.Instance.ActiveProfile.FramingAssistantSettings.CameraWidth;
            }
            set {
                ProfileManager.Instance.ActiveProfile.FramingAssistantSettings.CameraWidth = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }

        public int CameraHeight {
            get {
                return ProfileManager.Instance.ActiveProfile.FramingAssistantSettings.CameraHeight;
            }
            set {
                ProfileManager.Instance.ActiveProfile.FramingAssistantSettings.CameraHeight = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }

        private FramingAssistantSource _framingAssistantSource;
        public FramingAssistantSource FramingAssistantSource {
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

        private FramingImageParameter _imageParameter;
        public FramingImageParameter ImageParameter {
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

        private async Task<bool> LoadImageFromDSS() {
            try {
                _statusUpdate.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblDownloading"] });

                var arcsecPerPix = Astrometry.ArcsecPerPixel(CameraPixelSize, FocalLength);
                var p = new DigitalSkySurveyParameters() {
                    Coordinates = this.Coordinates,
                    FoV = Astrometry.DegreeToArcmin(FieldOfView)
                };

                var interaction = new DigitalSkySurveyInteraction(DigitalSkySurveyDomain.NASA);
                var img = await interaction.Download(p, _loadImageSource.Token, _progress);
                var parameter = new FramingImageParameter() {
                    Image = img,
                    FieldOfViewWidth = FieldOfView,
                    FieldOfViewHeight = FieldOfView,
                    Rotation = 180
                };

                CalculateRectangle(parameter);

                await _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    ImageParameter = parameter;
                }));
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            } finally {
                _statusUpdate.Report(new ApplicationStatus() { Status = "" });
            }
            return true;

        }

        private async Task<bool> LoadImage() {
            CancelLoadImage();
            _loadImageSource = new CancellationTokenSource();

            if (FramingAssistantSource == FramingAssistantSource.DSS) {
                var success = await LoadImageFromDSS();
                if (success) {
                    FillImageCache();
                }
            } else if (FramingAssistantSource == FramingAssistantSource.FILE) {
                var success = await LoadImageFromFile();
                if (success) {
                    FillImageCache();
                }
            } else if (FramingAssistantSource == FramingAssistantSource.CACHE) {
                await LoadImageFromCache();
            } else {
                return false;
            }
            return true;
        }

        private async Task LoadImageFromCache() {
            if (SelectedImageCacheInfo != null) {
                var img = LoadJpg(SelectedImageCacheInfo.Attribute("FileName").Value);
                var fovW = double.Parse(SelectedImageCacheInfo.Attribute("FoVW").Value, CultureInfo.InvariantCulture);
                var fovH = double.Parse(SelectedImageCacheInfo.Attribute("FoVH").Value, CultureInfo.InvariantCulture);
                var rotation = double.Parse(SelectedImageCacheInfo.Attribute("Rotation").Value, CultureInfo.InvariantCulture);
                var parameter = new FramingImageParameter() {
                    Image = img,
                    FieldOfViewWidth = fovW,
                    FieldOfViewHeight = fovH,
                    Rotation = rotation
                };
                var ra = double.Parse(SelectedImageCacheInfo.Attribute("RA").Value, CultureInfo.InvariantCulture);
                var dec = double.Parse(SelectedImageCacheInfo.Attribute("Dec").Value, CultureInfo.InvariantCulture);
                Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours);
                DSO.Name = SelectedImageCacheInfo.Attribute("Name").Value;
                FieldOfView = Math.Round(Math.Max(parameter.FieldOfViewWidth, parameter.FieldOfViewHeight), 2);
                CalculateRectangle(parameter);
                await _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    ImageParameter = parameter;
                }));
            }

        }

        private void FillImageCache() {
            try {
                if (!Directory.Exists(FRAMINGASSISTANTCACHEPATH)) {
                    Directory.CreateDirectory(FRAMINGASSISTANTCACHEPATH);
                }

                var imgFilePath = Path.Combine(FRAMINGASSISTANTCACHEPATH, DSO.Name + ".jpg");

                imgFilePath = Utility.Utility.GetUniqueFilePath(imgFilePath);
                var name = Path.GetFileNameWithoutExtension(imgFilePath);

                using (var fileStream = new FileStream(imgFilePath, FileMode.Create)) {
                    var encoder = new JpegBitmapEncoder();
                    encoder.QualityLevel = 80;
                    encoder.Frames.Add(BitmapFrame.Create(ImageParameter.Image));
                    encoder.Save(fileStream);
                }

                XElement xml = new XElement("Image",
                    new XAttribute("RA", Coordinates.RA),
                    new XAttribute("Dec", Coordinates.Dec),
                    new XAttribute("Rotation", ImageParameter.Rotation),
                    new XAttribute("FoVW", ImageParameter.FieldOfViewWidth),
                    new XAttribute("FoVH", ImageParameter.FieldOfViewHeight),
                    new XAttribute("FileName", imgFilePath),
                    new XAttribute("Name", name)
                );

                ImageCacheInfo.Add(xml);
                ImageCacheInfo.Save(FRAMINGASSISTANTCACHEINFOPATH);
                SelectedImageCacheInfo = xml;
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }

        }

        private XElement _imageCacheInfo;
        public XElement ImageCacheInfo {
            get {
                return _imageCacheInfo;
            }
            set {
                _imageCacheInfo = value;
                RaisePropertyChanged();
            }
        }

        private XElement _selectedImageCacheInfo;
        public XElement SelectedImageCacheInfo {
            get {
                return _selectedImageCacheInfo;
            }
            set {
                _selectedImageCacheInfo = value;
                RaisePropertyChanged();
            }
        }

        private void LoadImageCacheList() {
            if (!Directory.Exists(FRAMINGASSISTANTCACHEPATH)) {
                Directory.CreateDirectory(FRAMINGASSISTANTCACHEPATH);
            }

            if (!File.Exists(FRAMINGASSISTANTCACHEINFOPATH)) {
                XElement info = new XElement("ImageCacheInfo");
                info.Save(FRAMINGASSISTANTCACHEINFOPATH);
                ImageCacheInfo = info;
                return;
            } else {
                ImageCacheInfo = XElement.Load(FRAMINGASSISTANTCACHEINFOPATH);
            }
            SelectedImageCacheInfo = (XElement)ImageCacheInfo.FirstNode;
        }

        private void CalculateRectangle(FramingImageParameter parameter) {
            if (parameter != null) {
                var imageArcsecWidth = Astrometry.DegreeToArcsec(parameter.FieldOfViewWidth) / parameter.Image.Width;
                var imageArcsecHeight = Astrometry.DegreeToArcsec(parameter.FieldOfViewHeight) / parameter.Image.Height;

                var arcsecPerPix = Astrometry.ArcsecPerPixel(CameraPixelSize, FocalLength);
                var conversion = arcsecPerPix / imageArcsecWidth;
                var width = CameraWidth * conversion;
                var height = CameraHeight * conversion;
                Rectangle = new ObservableRectangle(parameter.Rotation) { Width = width, Height = height, X = parameter.Image.Width / 2d - width / 2d, Y = parameter.Image.Height / 2d - height / 2d, Rotation = Rectangle?.Rotation ?? 0 };
                SelectedCoordinates = new Coordinates(Coordinates.RA, Coordinates.Dec, Epoch.J2000, Coordinates.RAType.Hours);
            }
        }



        private void DragStart(object obj) {
        }

        private void DragStop(object obj) {

        }

        private void DragMove(object obj) {
            var delta = (Vector)obj;
            this.Rectangle.X += delta.X;
            this.Rectangle.Y += delta.Y;

            var orientation = Astrometry.ToRadians(ImageParameter.Rotation);
            var x = delta.X * Math.Cos(orientation) + delta.Y * Math.Sin(orientation);
            var y = delta.Y * Math.Cos(orientation) - delta.X * Math.Sin(orientation);

            var imageArcsecWidth = Astrometry.DegreeToArcsec(ImageParameter.FieldOfViewWidth) / ImageParameter.Image.Width;
            var imageArcsecHeight = Astrometry.DegreeToArcsec(ImageParameter.FieldOfViewHeight) / ImageParameter.Image.Height;

            SelectedCoordinates = new Coordinates(
                SelectedCoordinates.RADegrees + Astrometry.ArcsecToDegree(x * imageArcsecWidth),
                SelectedCoordinates.Dec + Astrometry.ArcsecToDegree(y * imageArcsecHeight),
                Epoch.J2000,
                Coordinates.RAType.Degrees
            );
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

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum FramingAssistantSource {
        [Description("LblDigitalSkySurvey")]
        DSS,
        [Description("LblFile")]
        FILE,
        [Description("LblCache")]
        CACHE
    }

    class FramingImageParameter {
        public BitmapSource Image { get; set; }
        public double FieldOfViewWidth { get; set; }
        public double FieldOfViewHeight { get; set; }
        public double Rotation { get; set; }
    }

    public class ObservableRectangle : BaseINPC {
        public ObservableRectangle(double rotationOffset) {
            _rotationOffset = rotationOffset;
        }

        private double _x;
        public double X {
            get {
                return _x;
            }
            set {
                _x = value;
                RaisePropertyChanged();
            }
        }
        private double _y;
        public double Y {
            get {
                return _y;
            }
            set {
                _y = value;
                RaisePropertyChanged();
            }
        }
        private double _width;
        public double Width {
            get {
                return _width;
            }
            set {
                _width = value;
                RaisePropertyChanged();
            }
        }
        private double _height;
        public double Height {
            get {
                return _height;
            }
            set {
                _height = value;
                RaisePropertyChanged();
            }
        }

        private double _rotation;
        public double Rotation {
            get {
                return _rotation;
            }
            set {
                _rotation = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DisplayedRotation));
            }
        }
        private double _rotationOffset;
        public double DisplayedRotation {
            get {
                var rotation = Rotation - _rotationOffset;
                if (rotation < 0) {
                    rotation += 360;
                } else if (rotation >= 360) {
                    rotation -= 360;
                }
                return Math.Round(rotation, 2);
            }
        }
    }
}
