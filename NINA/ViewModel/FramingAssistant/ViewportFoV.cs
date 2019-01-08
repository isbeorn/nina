using NINA.Utility.Astrometry;
using System;
using System.Windows;

namespace NINA.ViewModel.FramingAssistant {

    public class ViewportFoV {
        public Coordinates AbsoluteCenterCoordinates { get; }
        public Coordinates TopCenter { get; }
        public Coordinates TopLeft { get; }
        public Coordinates BottomLeft { get; }
        public Coordinates CenterCoordinates { get; }
        public double CalcTopDec { get; }
        public double CalcBotomDec { get; }
        public double VFoVDegTop { get; }
        public double VFoVDegBottom { get; }
        public double VFoVDeg => VFoVDegTop + VFoVDegBottom;
        public double HFoVDeg { get; }
        public bool AboveZero { get; } = true;
        public bool IsAbove90 { get; }
        public double ArcSecWidth { get; }
        public double ArcSecHeight { get; }
        public Point ViewPortCenterPoint { get; }
        public double Rotation { get; }

        public ViewportFoV(Coordinates centerCoordinates, double vFoVDegrees, double width, double height, double rotation) {
            var verticalFov = vFoVDegrees;
            var horizontalFov = (width / height) * vFoVDegrees;

            CenterCoordinates = centerCoordinates;

            Rotation = rotation;

            ViewPortCenterPoint = new Point(width / 2, height / 2);

            ArcSecWidth = Astrometry.DegreeToArcsec(horizontalFov) / width;
            ArcSecHeight = Astrometry.DegreeToArcsec(verticalFov) / height;

            AbsoluteCenterCoordinates = new Coordinates(centerCoordinates.RADegrees, Math.Abs(centerCoordinates.Dec), Epoch.J2000, Coordinates.RAType.Degrees);

            TopCenter = AbsoluteCenterCoordinates.Shift(0, -verticalFov / 2, 0);
            TopLeft = AbsoluteCenterCoordinates.Shift(-horizontalFov / 2, (-verticalFov / 2), 0);
            BottomLeft = AbsoluteCenterCoordinates.Shift(-horizontalFov / 2, (verticalFov / 2), 0);

            VFoVDegTop = Math.Abs(TopCenter.Dec - AbsoluteCenterCoordinates.Dec);
            VFoVDegBottom = Math.Abs(AbsoluteCenterCoordinates.Dec - BottomLeft.Dec);

            HFoVDeg = TopLeft.RADegrees > TopCenter.RADegrees
                ? TopLeft.RADegrees - TopCenter.RADegrees
                : TopLeft.RADegrees - TopCenter.RADegrees + 360;
            HFoVDeg *= 2;

            // if the bottom left point is below 0 assume fov for bottom is the same as for top
            if (BottomLeft.Dec < 0) {
                VFoVDegBottom = VFoVDegTop;
            }

            // if we're below 0 we need to flip all calculated decs
            if (centerCoordinates.Dec < 0) {
                BottomLeft.Dec *= -1;
                TopLeft.Dec *= -1;
                TopCenter.Dec *= -1;
                AboveZero = false;
            }

            if (Math.Abs(TopCenter.RADegrees - centerCoordinates.RADegrees) > 0.0001) {
                // horizontal fov becomes 360 here and vertical fov the difference between center and 90deg
                HFoVDeg = 360;
                VFoVDegTop = 90 - AbsoluteCenterCoordinates.Dec;
                IsAbove90 = true;
            }

            CalcTopDec = AbsoluteCenterCoordinates.Dec + VFoVDegTop;
            CalcBotomDec = AbsoluteCenterCoordinates.Dec - VFoVDegBottom;
        }
    }
}