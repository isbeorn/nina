using NINA.Utility.Astrometry;
using System;
using System.Windows;
using Point = System.Windows.Point;

namespace NINA.ViewModel.FramingAssistant {

    public class ViewportFoV {
        public Coordinates AbsoluteCenterCoordinates { get; private set; }
        public Coordinates TopCenter { get; private set; }
        public Coordinates TopLeft { get; private set; }
        public Coordinates BottomLeft { get; private set; }
        public Coordinates CenterCoordinates { get; private set; }
        public double CalcTopDec { get; private set; }
        public double CalcBotomDec { get; private set; }
        public double VFoVDegTop { get; private set; }
        public double VFoVDegBottom { get; private set; }
        public double VFoVDeg => VFoVDegTop + VFoVDegBottom;
        public double HFoVDeg { get; private set; }
        public bool AboveZero { get; private set; }
        public bool IsAbove90 { get; private set; }
        public double ArcSecWidth { get; }
        public double ArcSecHeight { get; }
        public Point ViewPortCenterPoint { get; }
        public double Rotation { get; }
        public double OriginalVFoV { get; }
        public double OriginalHFoV { get; }

        public ViewportFoV(Coordinates centerCoordinates, double vFoVDegrees, double width, double height, double rotation) {
            OriginalVFoV = vFoVDegrees;
            OriginalHFoV = (width / height) * vFoVDegrees;

            CenterCoordinates = centerCoordinates;

            Rotation = rotation;

            ViewPortCenterPoint = new Point(width / 2, height / 2);

            ArcSecWidth = Astrometry.DegreeToArcsec(OriginalHFoV) / width;
            ArcSecHeight = Astrometry.DegreeToArcsec(OriginalVFoV) / height;

            Shift(new Vector(0, 0));
        }

        public void Shift(Vector delta) {
            if (delta.X == 0 && delta.Y == 0 && AbsoluteCenterCoordinates != null) {
                return;
            }

            CenterCoordinates = CenterCoordinates.Shift(delta.X, delta.Y, Rotation, ArcSecWidth, ArcSecHeight);

            AbsoluteCenterCoordinates = new Coordinates(CenterCoordinates.RADegrees, Math.Abs(CenterCoordinates.Dec), Epoch.J2000, Coordinates.RAType.Degrees);

            TopCenter = AbsoluteCenterCoordinates.Shift(0, -OriginalVFoV / 2, 0);
            TopLeft = AbsoluteCenterCoordinates.Shift(-OriginalHFoV / 2, (-OriginalVFoV / 2), 0);
            BottomLeft = AbsoluteCenterCoordinates.Shift(-OriginalHFoV / 2, (OriginalVFoV / 2), 0);

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
            if (CenterCoordinates.Dec < 0) {
                BottomLeft.Dec *= -1;
                TopLeft.Dec *= -1;
                TopCenter.Dec *= -1;
                AboveZero = false;
            } else {
                AboveZero = true;
            }

            // if the top center ra is different than the center coordinate ra then the top center point is above 90deg
            if (Math.Abs(TopCenter.RADegrees - CenterCoordinates.RADegrees) > 0.0001) {
                // horizontal fov becomes 360 here and vertical fov the difference between center and 90deg
                HFoVDeg = 360;
                VFoVDegTop = 90 - AbsoluteCenterCoordinates.Dec;
                IsAbove90 = true;
            } else {
                IsAbove90 = false;
            }

            CalcTopDec = AbsoluteCenterCoordinates.Dec + VFoVDegTop;
            CalcBotomDec = AbsoluteCenterCoordinates.Dec - VFoVDegBottom;
        }
    }
}