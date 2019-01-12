using NINA.Utility.Astrometry;
using System.Windows;
using Math = System.Math;
using Point = System.Windows.Point;

namespace NINA.ViewModel.FramingAssistant {

    public class ViewportFoV {
        public Coordinates AbsoluteCenterCoordinates { get; private set; }
        public Coordinates TopCenter { get; private set; }
        public Coordinates TopLeft { get; private set; }
        public Coordinates BottomLeft { get; private set; }
        public Coordinates CenterCoordinates { get; private set; }
        public double AbsCalcTopDec { get; private set; }
        public double AbsCalcBottomDec { get; private set; }
        public double CalcTopDec { get; private set; }
        public double CalcBottomDec { get; private set; }
        public double VFoVDegTop { get; private set; }
        public double VFoVDegBottom { get; private set; }
        public double VFoVDeg => VFoVDegTop + VFoVDegBottom;
        public double HFoVDeg { get; private set; }
        public bool AboveZero { get; private set; }
        public bool IsAbove90 { get; private set; }
        public double Width { get; }
        public double Height { get; }
        public double ArcSecWidth { get; }
        public double ArcSecHeight { get; }
        public Point ViewPortCenterPoint { get; }
        public double Rotation { get; }
        public double OriginalVFoV { get; }
        public double OriginalHFoV { get; }
        public double CalcRAMin { get; private set; }
        public double CalcRAMax { get; private set; }
        private readonly double horizontalBoundsPadding;
        private readonly double verticalBoundsPadding;

        public ViewportFoV(Coordinates centerCoordinates, double vFoVDegrees, double width, double height, double rotation) {
            Rotation = rotation;

            // I don't know why you need to use -Rotation-90 but this is what works
            (Width, Height) = GetBoundingBoxOfRotatedRectangle(-Rotation - 90, width, height);

            (OriginalHFoV, OriginalVFoV) = GetBoundingBoxOfRotatedRectangle(-Rotation - 90, (width / height) * vFoVDegrees, vFoVDegrees);

            ArcSecWidth = Astrometry.DegreeToArcsec(OriginalHFoV) / Width;
            ArcSecHeight = Astrometry.DegreeToArcsec(OriginalVFoV) / Height;

            CenterCoordinates = centerCoordinates;

            ViewPortCenterPoint = new Point(width / 2, height / 2);

            Shift(new Vector(0, 0));

            horizontalBoundsPadding = Width / 6;
            verticalBoundsPadding = Height / 6;
        }

        private (double width, double height) GetBoundingBoxOfRotatedRectangle(double rotation, double width, double height) {
            if (rotation == 0) {
                return (width, height);
            }
            var rot = Astrometry.ToRadians(rotation);

            return (Math.Abs(width * Math.Sin(rot) + height * Math.Cos(rot)), Math.Abs(height * Math.Sin(rot) + width * Math.Cos(rot)));
        }

        public bool ContainsCoordinates(Coordinates coordinates) {
            return ContainsCoordinates(coordinates.RADegrees, coordinates.Dec);
        }

        public bool ContainsCoordinates(double ra, double dec) {
            return ((CalcRAMin > CalcRAMax && (ra > CalcRAMin || ra < CalcRAMax)) // case viewport is going over 0
                    || (ra < CalcRAMax && ra > CalcRAMin) // case viewport is "normal"
                    || IsAbove90 // case dec viewport is above 90deg
                    ) && ((!AboveZero && dec < CalcBottomDec && dec > CalcTopDec) || (AboveZero && dec > CalcBottomDec && dec < CalcTopDec)); // is in between dec
        }

        public bool IsOutOfViewportBounds(Point point) {
            return (point.X < -1 * horizontalBoundsPadding ||
                     point.X > Width + horizontalBoundsPadding ||
                     point.Y < -1 * verticalBoundsPadding ||
                     point.Y > Height + verticalBoundsPadding);
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

            AbsCalcTopDec = AbsoluteCenterCoordinates.Dec + VFoVDegTop;
            AbsCalcBottomDec = AbsoluteCenterCoordinates.Dec - VFoVDegBottom;

            if (AboveZero) {
                CalcTopDec = CenterCoordinates.Dec + VFoVDegTop;
                CalcBottomDec = CenterCoordinates.Dec - VFoVDegBottom;
            } else {
                CalcTopDec = CenterCoordinates.Dec - VFoVDegTop;
                CalcBottomDec = CenterCoordinates.Dec + VFoVDegBottom;
            }

            CalcRAMax = TopLeft.RADegrees;
            CalcRAMin = CalcRAMax - HFoVDeg;
            if (CalcRAMin < 0) {
                CalcRAMin += 360;
            }
        }
    }
}