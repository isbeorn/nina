using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class FramingAssistantVM : BaseVM {

        public FramingAssistantVM() {
            Coordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);
            //Coordinates = new Coordinates(073.2920, -07.6335, Epoch.J2000, Coordinates.RAType.Degrees);
            //Coordinates = new Coordinates(10.6833, 41.2686, Epoch.J2000, Coordinates.RAType.Degrees);

            FieldOfView = 3;
            CameraWidth = 1500;
            CameraHeight = 1000;
            CameraPixelSize = Settings.CameraPixelSize;
            FocalLength = Settings.TelescopeFocalLength;
            LoadImageCommand = new AsyncCommand<bool>(async () => { return await LoadImage(); });
            _progress = new Progress<int>((p) => DownloadProgressValue = p);
            CancelLoadImageCommand = new RelayCommand((object o) => { CancelLoadImage(); });
            DragStartCommand = new RelayCommand(DragStart);
            DragStopCommand = new RelayCommand(DragStop);
            DragMoveCommand = new RelayCommand(DragMove);
            SetSequenceCoordinatesCommand = new AsyncCommand<bool>(async () => {
                return await Mediator.Instance.RequestAsync(new SetSequenceCoordinatesMessage() { DSO = new DeepSkyObject(string.Empty, SelectedCoordinates) });
            }, (object o) => SelectedCoordinates != null);

            RecenterCommand = new AsyncCommand<bool>(async () => {
                Coordinates = SelectedCoordinates;
                await LoadImageCommand.ExecuteAsync(null);
                return true;
            }, (object o) => SelectedCoordinates != null);

            Mediator.Instance.RegisterAsyncRequest(new SetFramingAssistantCoordinatesMessageHandle(async (SetFramingAssistantCoordinatesMessage m) => {
                Mediator.Instance.Request(new ChangeApplicationTabMessage() { Tab = ApplicationTab.FRAMINGASSISTANT });
                this.Coordinates = m.DSO.Coordinates;
                await LoadImageCommand.ExecuteAsync(null);
                return true;
            }));
        }

        private void CancelLoadImage() {
            _loadImageSource?.Cancel();
        }

        Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

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
                return (int)Math.Abs(Math.Truncate(_coordinates.RA));
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
                return (int)Math.Abs(Math.Truncate((_coordinates.RA - RAHours) * 60));
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
                return (int)Math.Abs(Math.Truncate((_coordinates.RA - RAHours - RAMinutes / 60.0d) * 60d * 60d));
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
                return (int)(Math.Truncate(_coordinates.Dec));
            }
            set {
                _coordinates.Dec = _coordinates.Dec - DecDegrees + value;
                RaiseCoordinatesChanged();
            }
        }

        public int DecMinutes {
            get {
                return (int)Math.Abs(Math.Truncate((_coordinates.Dec - DecDegrees) * 60));
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
                if (_coordinates.Dec >= 0) {
                    return (int)Math.Abs(Math.Truncate((_coordinates.Dec - DecDegrees - DecMinutes / 60.0d) * 60d * 60d));
                } else {
                    return (int)Math.Abs(Math.Truncate((_coordinates.Dec - DecDegrees + DecMinutes / 60.0d) * 60d * 60d));
                }
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

        private double _fieldOfView;
        public double FieldOfView {
            get {
                return _fieldOfView;
            }
            set {
                _fieldOfView = value;
                RaisePropertyChanged();
            }
        }

        private int _cameraWidth;
        public int CameraWidth {
            get {
                return _cameraWidth;
            }
            set {
                _cameraWidth = value;
                RaisePropertyChanged();
                CalculateRectangle(Image);
            }
        }

        private int _cameraHeight;
        public int CameraHeight {
            get {
                return _cameraHeight;
            }
            set {
                _cameraHeight = value;
                RaisePropertyChanged();
                CalculateRectangle(Image);
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
                CalculateRectangle(Image);
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
                CalculateRectangle(Image);
            }
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

        private IProgress<string> _statusUpdate = new Progress<string>((p) => {
            Mediator.Instance.Request(new StatusUpdateMessage() {
                Status = new ApplicationStatus() {
                    Source = Locale.Loc.Instance["LblFramingAssistant"],
                    Status = string.IsNullOrEmpty(p) ? "" : Locale.Loc.Instance[p]
                }
            });
        });

        private async Task<bool> LoadImage() {
            try {
                _statusUpdate.Report("LblDownloading");

                CancelLoadImage();
                _loadImageSource = new CancellationTokenSource();

                var arcsecPerPix = Astrometry.ArcsecPerPixel(CameraPixelSize, FocalLength);
                var p = new DigitalSkySurveyParameters() {
                    Coordinates = this.Coordinates,
                    FoV = Astrometry.DegreeToArcmin(FieldOfView)
                };

                var interaction = new DigitalSkySurveyInteraction(DigitalSkySurveyDomain.NASA);
                var img = await interaction.Download(p, _loadImageSource.Token, _progress);

                CalculateRectangle(img);

                await _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    Image = null;
                    GC.Collect();
                    Image = img;
                }));
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex.Message, ex.StackTrace);
                Notification.ShowError(ex.Message);
            } finally {
                _statusUpdate.Report("");
            }
            return true;

        }

        private void CalculateRectangle(BitmapSource img) {
            if (img != null) {
                var imageArcsecWidth = Astrometry.DegreeToArcsec(FieldOfView) / img.Width;
                var imageArcsecHeight = Astrometry.DegreeToArcsec(FieldOfView) / img.Height;

                var arcsecPerPix = Astrometry.ArcsecPerPixel(CameraPixelSize, FocalLength);
                var conversion = arcsecPerPix / imageArcsecWidth;
                var width = CameraWidth * conversion;
                var height = CameraHeight * conversion;
                Rectangle = new ObservableRectangle() { Width = width, Height = height, X = img.Width / 2d - width / 2d, Y = img.Height / 2d - height / 2d, Rotation = Rectangle?.Rotation ?? 0 };
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

            var imageArcsecWidth = Astrometry.DegreeToArcsec(FieldOfView) / Image.Width;
            var imageArcsecHeight = Astrometry.DegreeToArcsec(FieldOfView) / Image.Height;

            SelectedCoordinates = new Coordinates(
                SelectedCoordinates.RADegrees - Astrometry.ArcsecToDegree(delta.X * imageArcsecWidth),
                SelectedCoordinates.Dec - Astrometry.ArcsecToDegree(delta.Y * imageArcsecHeight),
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
        public IAsyncCommand RecenterCommand { get; private set; }
    }

    public class ObservableRectangle : BaseINPC {
        public ObservableRectangle() {
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
            }
        }
    }
}
