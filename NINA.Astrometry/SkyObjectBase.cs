using NINA.Astrometry.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.Astrometry {
    public abstract class SkyObjectBase : BaseINPC, IDeepSkyObject {
        [Obsolete]
        protected SkyObjectBase(string id, string imageRepository, CustomHorizon customHorizon) : this(id, null as Func<SkyObjectBase, Task<BitmapSource>>, customHorizon) {
        }

        protected SkyObjectBase(string id, Func<SkyObjectBase, Task<BitmapSource>> imageFactory, CustomHorizon customHorizon) {
            Id = id;
            Name = id;
            this.customHorizon = customHorizon;
            this.imageFactory = imageFactory;
        }

        private string id;

        public string Id {
            get => id;
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        public string Name {
            get => _name;
            set {
                _name = value;
                RaisePropertyChanged();
                RaisePropertyChanged("NameAsAscii");
            }
        }

        public string NameAsAscii => TextEncoding.UnicodeToAscii(TextEncoding.GreekToLatinAbbreviation(_name));

        public abstract Coordinates Coordinates { get; set; }
        public abstract Coordinates CoordinatesAt(DateTime at);
        public abstract SiderealShiftTrackingRate ShiftTrackingRate { get; }
        public abstract SiderealShiftTrackingRate ShiftTrackingRateAt(DateTime at);

        private string _dSOType;

        public string DSOType {
            get => _dSOType;
            set {
                _dSOType = value;
                RaisePropertyChanged();
            }
        }

        private string _constellation;

        public string Constellation {
            get => _constellation;
            set {
                _constellation = value;
                RaisePropertyChanged();
            }
        }

        private double? _magnitude;

        public double? Magnitude {
            get => _magnitude;
            set {
                _magnitude = value;
                RaisePropertyChanged();
            }
        }

        private Angle _positionAngle;

        public Angle PositionAngle {
            get => _positionAngle;
            set {
                _positionAngle = value;
                RaisePropertyChanged();
            }
        }

        private double? _sizeMin;

        public double? SizeMin {
            get => _sizeMin;
            set {
                _sizeMin = value;
                RaisePropertyChanged();
            }
        }

        private double? _size;

        public double? Size {
            get => _size;
            set {
                _size = value;
                RaisePropertyChanged();
            }
        }

        private double? _surfaceBrightness;

        public double? SurfaceBrightness {
            get => _surfaceBrightness;
            set {
                _surfaceBrightness = value;
                RaisePropertyChanged();
            }
        }

        [Obsolete("Use RotationPositionAngle instead")]
        public double Rotation {
            get => AstroUtil.EuclidianModulus(360 - RotationPositionAngle, 360);
            set {
                RotationPositionAngle = AstroUtil.EuclidianModulus(360 - value, 360);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RotationPositionAngle));
            }
        }

        private double rotationRotationPositionAngle;

        public double RotationPositionAngle {
            get => rotationRotationPositionAngle;
            set {
                rotationRotationPositionAngle = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Rotation));
            }
        }

        private DataPoint _maxAltitude;

        public DataPoint MaxAltitude {
            get => _maxAltitude;
            protected set {
                _maxAltitude = value;
                RaisePropertyChanged();
            }
        }

        private List<DataPoint> _altitudes;

        public List<DataPoint> Altitudes {
            get {
                if (_altitudes == null) {
                    _altitudes = new List<DataPoint>();
                    UpdateHorizonAndTransit();
                }
                return _altitudes;
            }
            private set {
                _altitudes = value;
                RaisePropertyChanged();
            }
        }

        private List<DataPoint> _horizon;

        public List<DataPoint> Horizon {
            get {
                if (_horizon == null) {
                    _horizon = new List<DataPoint>();
                }
                return _horizon;
            }
            private set {
                _horizon = value;
                RaisePropertyChanged();
            }
        }

        private List<string> _alsoKnownAs;

        public List<string> AlsoKnownAs {
            get {
                if (_alsoKnownAs == null) {
                    _alsoKnownAs = new List<string>();
                }
                return _alsoKnownAs;
            }
            set {
                _alsoKnownAs = value;
                RaisePropertyChanged();
            }
        }

        protected DateTime _referenceDate = DateTime.UtcNow;
        protected double _latitude;
        protected double _longitude;

        public void SetDateAndPosition(DateTime start, double latitude, double longitude) {
            this._referenceDate = start;
            this._latitude = latitude;
            this._longitude = longitude;
            this._altitudes = null;
        }

        public void SetCustomHorizon(CustomHorizon customHorizon) {
            this.customHorizon = customHorizon;
            this.UpdateHorizonAndTransit();
        }

        protected abstract void UpdateHorizonAndTransit();

        private bool _doesTransitSouth;

        public bool DoesTransitSouth {
            get => _doesTransitSouth;
            protected set {
                _doesTransitSouth = value;
                RaisePropertyChanged();
            }
        }

        //const string DSS_URL = "https://archive.stsci.edu/cgi-bin/dss_search";

        private Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        private BitmapSource _image;
        protected CustomHorizon customHorizon;

        private Func<SkyObjectBase, Task<BitmapSource>> imageFactory;

        public BitmapSource Image {
            get {
                if (_image == null) {
                    if(imageFactory != null) {
                        _ = Task.Run(async () => {
                            _image = await Task.Run(() => imageFactory(this));
                            _image.Freeze();
                            RaisePropertyChanged(nameof(Image));
                        });
                    }
                    /*var size = Astrometry.ArcsecToArcmin(this.Size ?? 300);
                    if (size > 25) { size = 25; }
                    size = Math.Max(15,size);
                    var path = string.Format(
                        "{0}?r={1}&d={2}&e=J2000&h={3}&w={4}&v=1&format=GIF",
                        DSS_URL,
                        this.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture),
                        this.Coordinates.Dec.ToString(CultureInfo.InvariantCulture),
                        (size * 9.0 / 16.0).ToString(CultureInfo.InvariantCulture),
                        size.ToString(CultureInfo.InvariantCulture));*/
                    //var file = Path.Combine(imageRepository, this.Id + ".gif");
                    //if (File.Exists(file)) {
                    //    _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    //        //var img = new BitmapImage(new Uri(file));
                    //        _image = new BitmapImage(new Uri(file)) { CacheOption = BitmapCacheOption.None, CreateOptions = BitmapCreateOptions.DelayCreation };
                    //        _image.Freeze();
                    //        RaisePropertyChanged(nameof(Image));
                    //    }));
                    //}
                }
                return _image;
            }
        }

        /*private Brush _imageBrush;
        public Brush ImageBrush {
            get {
                if(_imageBrush == null) {
                    _imageBrush = new ImageBrush(Image);
                }
                return _imageBrush;
            }
        }*/

        /*private void Img_DownloadCompleted(object sender,EventArgs e) {
            var path = "D:\\img\\";
            using (FileStream fs = new FileStream(path + this.Name + ".gif",FileMode.Create)) {
                var encoder = new GifBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(Image));
                encoder.Save(fs);
            }

            RaisePropertyChanged(nameof(Image));
        }*/
    }
}
