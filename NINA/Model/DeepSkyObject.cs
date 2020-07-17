#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Astrometry;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.Model {

    public class DeepSkyObject : BaseINPC {

        private DeepSkyObject(string imageRepository) {
            this.imageRepository = imageRepository;
        }

        public DeepSkyObject(string id, string imageRepository) : this(imageRepository) {
            Id = id;
            Name = id;
        }

        public DeepSkyObject(string id, Coordinates coords, string imageRepository) : this(id, imageRepository) {
            _coordinates = coords;
        }

        private string id;

        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates _coordinates;

        public Coordinates Coordinates {
            get {
                return _coordinates;
            }
            set {
                _coordinates = value;
                CalculateAltitude();
                RaisePropertyChanged();
            }
        }

        private string _dSOType;

        public string DSOType {
            get {
                return _dSOType;
            }
            set {
                _dSOType = value;
                RaisePropertyChanged();
            }
        }

        private string _constellation;

        public string Constellation {
            get {
                return _constellation;
            }
            set {
                _constellation = value;
                RaisePropertyChanged();
            }
        }

        private double? _magnitude;

        public double? Magnitude {
            get {
                return _magnitude;
            }
            set {
                _magnitude = value;
                RaisePropertyChanged();
            }
        }

        private double? _size;

        public double? Size {
            get {
                return _size;
            }
            set {
                _size = value;
                RaisePropertyChanged();
            }
        }

        private double? _surfaceBrightness;

        public double? SurfaceBrightness {
            get {
                return _surfaceBrightness;
            }
            set {
                _surfaceBrightness = value;
                RaisePropertyChanged();
            }
        }

        private double rotation;

        public double Rotation {
            get {
                return rotation;
            }
            set {
                rotation = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _maxAltitude;

        public DataPoint MaxAltitude {
            get {
                return _maxAltitude;
            }
            set {
                _maxAltitude = value;
                RaisePropertyChanged();
            }
        }

        private List<DataPoint> _altitudes;

        public List<DataPoint> Altitudes {
            get {
                if (_altitudes == null) {
                    _altitudes = new List<DataPoint>();
                    CalculateAltitude();
                }
                return _altitudes;
            }
            set {
                _altitudes = value;
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

        private DateTime _referenceDate = DateTime.UtcNow;
        private double _latitude;
        private double _longitude;

        public void SetDateAndPosition(DateTime start, double latitude, double longitude) {
            this._referenceDate = start;
            this._latitude = latitude;
            this._longitude = longitude;
            this._altitudes = null;
        }

        private void CalculateAltitude() {
            var start = this._referenceDate;
            Altitudes.Clear();
            var siderealTime = Astrometry.GetLocalSiderealTime(start, _longitude);
            var hourAngle = Astrometry.GetHourAngle(siderealTime, this.Coordinates.RA);

            for (double angle = hourAngle; angle < hourAngle + 24; angle += 0.1) {
                var degAngle = Astrometry.HoursToDegrees(angle);
                var altitude = Astrometry.GetAltitude(degAngle, _latitude, this.Coordinates.Dec);
                Altitudes.Add(new DataPoint(DateTimeAxis.ToDouble(start), altitude));
                start = start.AddHours(0.1);
            }

            MaxAltitude = Altitudes.OrderByDescending((x) => x.Y).FirstOrDefault();

            CalculateTransit(_latitude);
        }

        private void CalculateTransit(double latitude) {
            var alt0 = Astrometry.GetAltitude(0, latitude, this.Coordinates.Dec);
            var alt180 = Astrometry.GetAltitude(180, latitude, this.Coordinates.Dec);
            double transit;
            if (alt0 > alt180) {
                transit = Astrometry.GetAzimuth(0, alt0, latitude, this.Coordinates.Dec);
            } else {
                transit = Astrometry.GetAzimuth(180, alt180, latitude, this.Coordinates.Dec);
            }
            DoesTransitSouth = Convert.ToInt32(transit) == 180;
        }

        private bool _doesTransitSouth;

        public bool DoesTransitSouth {
            get {
                return _doesTransitSouth;
            }
            private set {
                _doesTransitSouth = value;
                RaisePropertyChanged();
            }
        }

        //const string DSS_URL = "https://archive.stsci.edu/cgi-bin/dss_search";

        private Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        private BitmapSource _image;
        private string imageRepository;

        public BitmapSource Image {
            get {
                if (_image == null) {
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
                    var file = Path.Combine(imageRepository, this.Id + ".gif");
                    if (File.Exists(file)) {
                        _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                            //var img = new BitmapImage(new Uri(file));
                            _image = new BitmapImage(new Uri(file)) { CacheOption = BitmapCacheOption.None, CreateOptions = BitmapCreateOptions.DelayCreation };
                            _image.Freeze();
                            RaisePropertyChanged(nameof(Image));
                        }));
                    }
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