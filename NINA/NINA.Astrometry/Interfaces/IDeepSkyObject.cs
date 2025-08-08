using NINA.Core.Model;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace NINA.Astrometry.Interfaces {
    public interface IDeepSkyObject : INotifyPropertyChanged {
        string Id { get; set; }
        string Name { get; set; }
        string NameAsAscii { get; }
        Coordinates Coordinates { get; set; }
        Coordinates CoordinatesAt(DateTime at);
        SiderealShiftTrackingRate ShiftTrackingRate { get; }
        SiderealShiftTrackingRate ShiftTrackingRateAt(DateTime at);
        string DSOType { get; set; }
        string Constellation { get; set; }
        double? Magnitude { get; set; }
        Angle PositionAngle { get; set; }
        double? SizeMin { get; set; }
        double? Size { get; set; }
        double? SurfaceBrightness { get; set; }

        [Obsolete("Use RotationPositionAngle instead")]
        double Rotation { get; set; }
        double RotationPositionAngle { get; set; }
        DataPoint MaxAltitude { get; }
        List<DataPoint> Altitudes { get; }
        List<DataPoint> Horizon { get; }
        List<string> AlsoKnownAs { get; set; }
        bool DoesTransitSouth { get; }
        BitmapSource Image { get; }

        void SetDateAndPosition(DateTime start, double latitude, double longitude);
        void SetCustomHorizon(CustomHorizon customHorizon);
    }
}
