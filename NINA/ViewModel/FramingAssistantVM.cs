using NINA.Utility;
using NINA.Utility.Astrometry;
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
            Coordinates = new Coordinates(83.822, -5.39, Epoch.J2000, Coordinates.RAType.Degrees);
            FieldOfView = 1;
            CameraPixelSize = Settings.CameraPixelSize;
            FocalLength = Settings.TelescopeFocalLength;
            LoadImageCommand = new AsyncCommand<bool>(async () => { return await LoadImage(); });
            CancelLoadImageCommand = new RelayCommand((object o) => { CancelLoadImage(); });
        }

        private void CancelLoadImage() {
            _loadImageSource?.Cancel();
        }

        const string DSS_URL = "https://archive.stsci.edu/cgi-bin/dss_search?r={0}&d={1}&e=J2000&h={2}&w={3}&v=1&format=GIF";
        Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        Coordinates _coordinates;
        public Coordinates Coordinates {
            get {
                return _coordinates;
            }
            set {
                _coordinates = value;
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

        private CancellationTokenSource _loadImageSource;

        private async Task<bool> LoadImage() {

            CancelLoadImage();
            _loadImageSource = new CancellationTokenSource();

            var url = string.Format(
                DSS_URL,
                this.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture),
                this.Coordinates.Dec.ToString(CultureInfo.InvariantCulture),
                Astrometry.DegreeToArcmin(FieldOfView),
                Astrometry.DegreeToArcmin(FieldOfView)
                );

            var img = await Utility.Utility.HttpGetImage(_loadImageSource.Token, url);

            CalculateRectangle(img);

            await _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {                
                Image = img;
            }));

            return true;
            
        }

        private void CalculateRectangle(BitmapSource img) {
            if(img != null) {
                var imageArcsecWidth = Astrometry.DegreeToArcsec(FieldOfView) / img.Width;
                var imageArcsecHeight = Astrometry.DegreeToArcsec(FieldOfView) / img.Height;

                var arcsecPerPix = Astrometry.ArcsecPerPixel(CameraPixelSize, FocalLength);
                var conversion = arcsecPerPix / imageArcsecWidth;
                var width = CameraWidth * conversion;
                var height = CameraHeight * conversion;
                Rectangle = new ObservableRectangle() { Width = width, Height = height, X = img.Width / 2d - width / 2d, Y = img.Height / 2d - height / 2d };
            }            
        }

        public ICommand LoadImageCommand { get; private set; }
        public ICommand CancelLoadImageCommand { get; private set; }
    }

    public class ObservableRectangle : BaseINPC {
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
