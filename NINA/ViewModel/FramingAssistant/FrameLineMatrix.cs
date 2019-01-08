using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NINA.ViewModel.FramingAssistant {

    public class FrameLineMatrix : BaseINPC {
        private readonly double[] RASTEPS = { 0.46875, 0.9375, 1.875, 3.75, 7.5, 15 };

        private readonly double[] DECSTEPS = { 1, 2, 4, 8, 16, 32, 64 };

        private const double MAXDEC = 89;

        public FrameLineMatrix() {
            RAPoints = new AsyncObservableCollection<PointCollectionAndClosed>();
            DecPoints = new AsyncObservableCollection<PointCollectionAndClosed>();
        }

        public void CalculatePoints(ViewportFoV calculations) {
            // calculate the lines based on fov height and current dec to avoid projection issues
            // atan gnomoric projection cannot project properly over 90deg, it will result in the same results as prior
            // and dec lines will overlap each other

            var realTopDec = calculations.CalcTopDec;
            var realBottomDec = calculations.CalcBotomDec;

            double raStep;
            double decStep;
            double raStart;
            double raStop;
            double decStart;
            double decStop;

            if (realTopDec >= MAXDEC) {
                realTopDec = MAXDEC;
                raStep = 30; // 12 lines at top
                decStep = 1;

                raStart = 0;
                raStop = 360 - raStep;
            } else {
                // we want at least 4 lines
                // get the steps that are closest to vFovDegTotal and closest to hFovDeg
                decStep = calculations.VFoVDeg / 4;
                decStep = DECSTEPS.OrderBy(item => Math.Abs(decStep - item)).First();

                raStep = calculations.HFoVDeg / 4;
                raStep = RASTEPS.OrderBy(item => Math.Abs(raStep - item)).First();

                while (realTopDec + decStep > MAXDEC) {
                    if (decStep == 1) {
                        realTopDec = MAXDEC;
                        raStep = 30;
                        break;
                    }

                    decStep = DECSTEPS[Array.FindIndex(DECSTEPS, w => w == decStep) - 1];
                }

                raStop = calculations.TopLeft.RADegrees - calculations.HFoVDeg < 0
                    ? RoundToHigherValue(calculations.TopLeft.RADegrees - calculations.HFoVDeg, raStep)
                    : RoundToLowerValue(calculations.TopLeft.RADegrees - calculations.HFoVDeg, raStep);
                raStart = RoundToHigherValue(calculations.TopLeft.RADegrees, raStep);
            }

            if (realTopDec == MAXDEC) {
                decStart = RoundToHigherValue(realBottomDec, decStep);
            } else {
                decStart = realBottomDec < 0
                    ? RoundToHigherValue(realBottomDec, decStep)
                    : RoundToLowerValue(realBottomDec, decStep);
            }

            decStop = RoundToHigherValue(realTopDec, decStep);
            if (decStop >= MAXDEC) {
                decStop = MAXDEC;
            }

            // flip coordinates if necessary, also consider 0 because Math.Sign(0) returns 0
            decStart *= calculations.AboveZero ? 1 : -1;
            decStop *= calculations.AboveZero ? 1 : -1;

            var pointsByDecDict = new Dictionary<double, PointCollectionAndClosed>();

            bool raClosed = raStop + raStep == 360;

            for (double ra = Math.Min(raStart, raStop);
                ra <= Math.Max(raStop, raStart);
                ra += raStep) {
                PointCollection raPointCollection = new PointCollection();

                for (double dec = Math.Min(decStart, decStop);
                    dec <= Math.Max(decStart, decStop);
                    dec += decStep) {
                    var point = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees).ProjectFromCenterToXY(calculations);

                    if (!pointsByDecDict.ContainsKey(dec)) {
                        pointsByDecDict.Add(dec, new PointCollectionAndClosed() { Closed = raClosed, Collection = new PointCollection(new List<Point> { point }) });
                    } else {
                        pointsByDecDict[dec].Collection.Add(point);
                    }

                    raPointCollection.Add(point);
                }

                // those are the vertical lines
                RAPoints.Add(new PointCollectionAndClosed {
                    Closed = false,
                    Collection = raPointCollection
                });
            }

            // those are actually the circles
            foreach (KeyValuePair<double, PointCollectionAndClosed> item in pointsByDecDict) {
                DecPoints.Add(item.Value);
            }
        }

        private AsyncObservableCollection<PointCollectionAndClosed> raPoints;
        private AsyncObservableCollection<PointCollectionAndClosed> decPoints;

        public AsyncObservableCollection<PointCollectionAndClosed> RAPoints {
            get { return raPoints; }
            set {
                raPoints = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<PointCollectionAndClosed> DecPoints {
            get { return decPoints; }
            set {
                decPoints = value;
                RaisePropertyChanged();
            }
        }

        public static double RoundToHigherValue(double value, double multiple) {
            return (Math.Abs(value) + (multiple - Math.Abs(value) % multiple)) * Math.Sign(value);
        }

        public static double RoundToLowerValue(double value, double multiple) {
            return (Math.Abs(value) - (Math.Abs(value) % multiple)) * Math.Sign(value);
        }
    }
}