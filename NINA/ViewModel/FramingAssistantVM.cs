using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
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
            
            CameraPixelSize = Settings.CameraPixelSize;
            FocalLength = Settings.TelescopeFocalLength;

            _statusUpdate = new Progress<ApplicationStatus>(p => Status = p);

            LoadImageCommand = new AsyncCommand<bool>(async () => { return await LoadImage(); });
            CancelLoadImageFromFileCommand = new RelayCommand((object o) => { CancelLoadImage(); });
            _progress = new Progress<int>((p) => DownloadProgressValue = p);
            CancelLoadImageCommand = new RelayCommand((object o) => { CancelLoadImage(); });
            DragStartCommand = new RelayCommand(DragStart);
            DragStopCommand = new RelayCommand(DragStop);
            DragMoveCommand = new RelayCommand(DragMove);
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
                if(SelectedCoordinates != null) {
                    return await Mediator.Instance.RequestAsync(new SlewToCoordinatesMessage() { Coordinates = SelectedCoordinates });
                }
                return false;
            }, (object o) => SelectedCoordinates != null);


            RegisterMediatorMessages();
            LoadImageCacheList();
            
        }

        public static string FRAMINGASSISTANTCACHEPATH = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "FramingAssistantCache");

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
                    case ".tif": case ".tiff":
                        img = LoadTiff(dialog.FileName);
                        break;
                    case ".png":
                        img = LoadPng(dialog.FileName);
                        break;
                    case ".jpg":
                        img = LoadJpg(dialog.FileName);
                        break;
                }
                
                if(img == null) {
                    return false;
                }

                var dialogResult = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblBlindSolveAttemptForFraming"], Locale.Loc.Instance["LblNoCoordinates"], MessageBoxButton.OKCancel, MessageBoxResult.OK);
                if(dialogResult == MessageBoxResult.OK) {
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
                _dSO?.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), Settings.Latitude, Settings.Longitude);
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
                if(value < 0) {
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
                return Settings.FramingAssistantFieldOfView;
            }
            set {
                Settings.FramingAssistantFieldOfView = value;
                RaisePropertyChanged();
            }
        }
        
        public int CameraWidth {
            get {
                return Settings.FramingAssistantCameraWidth;
            }
            set {
                Settings.FramingAssistantCameraWidth = value;
                RaisePropertyChanged();
                CalculateRectangle(ImageParameter);
            }
        }
        
        public int CameraHeight {
            get {
                return Settings.FramingAssistantCameraHeight;
            }
            set {
                Settings.FramingAssistantCameraHeight = value;
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
                await LoadImageFromDSS();
                await FillImageCache();
            } else if (FramingAssistantSource == FramingAssistantSource.FILE) {
                await LoadImageFromFile();
                await FillImageCache();
            } else if (FramingAssistantSource == FramingAssistantSource.CACHE) {
                await LoadImageFromCache();
            } else {
                return false;
            }
            return true;
        }

        private async Task LoadImageFromCache() {
            var img = LoadTiff(@"C:\Users\Isbeorn\AppData\Local\NINA\FramingAssistantCache\B78.tif");
            var parameter = new FramingImageParameter() {
                Image = img,
                FieldOfViewWidth = Astrometry.ArcsecToDegree(3 * img.Width),
                FieldOfViewHeight = Astrometry.ArcsecToDegree(3 * img.Height),
                Rotation = 0
            };
            //Coordinates = plateSolveResult.Coordinates;
            FieldOfView = Math.Round(Math.Max(parameter.FieldOfViewWidth, parameter.FieldOfViewHeight), 2);
            CalculateRectangle(parameter);
            await _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                ImageParameter = parameter;
            }));
        }

        private async Task FillImageCache() {
            if(!Directory.Exists(FRAMINGASSISTANTCACHEPATH)) {
                Directory.CreateDirectory(FRAMINGASSISTANTCACHEPATH);
            }

            var xmlFile = Path.Combine(FRAMINGASSISTANTCACHEPATH, DSO.Name + ".xml");
            var tiffFile = Path.Combine(FRAMINGASSISTANTCACHEPATH, DSO.Name + ".tif");

            XElement xml = new XElement("Image",
                new XElement("Coordinates",
                    new XAttribute("RA", Coordinates.RA),
                    new XAttribute("Dec", Coordinates.Dec),
                    new XAttribute("FoVW", ImageParameter.FieldOfViewWidth),
                    new XAttribute("FoVH", ImageParameter.FieldOfViewHeight),
                    new XAttribute("Rotation", ImageParameter.Rotation)
                ),
                new XAttribute("FileName", tiffFile)
            );

            xml.Save(xmlFile);

            using (var fileStream = new FileStream(tiffFile, FileMode.Create)) {
                BitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(ImageParameter.Image));
                encoder.Save(fileStream);
            }
        }

        private void LoadImageCacheList() {
            if(Directory.Exists(FRAMINGASSISTANTCACHEPATH)) {
                foreach(string fileName in Directory.GetFiles(FRAMINGASSISTANTCACHEPATH)) {
                    //todo
                }
            }
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
