#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NINA.ViewModel.FramingAssistant {

    public class FrameLineMatrix {
        private readonly double[] RASTEPS = { 0.46875, 0.9375, 1.875, 3.75, 7.5, 15 };

        private readonly double[] DECSTEPS = { 1, 2, 4, 8, 16, 32, 64 };

        private const double MAXDEC = 89;

        public FrameLineMatrix() {
            RAPoints = new List<FrameLine>();
            DecPoints = new List<FrameLine>();
        }

        public void CalculatePoints(ViewportFoV viewport) {
            // calculate the lines based on fov height and current dec to avoid projection issues
            // atan gnomoric projection cannot project properly over 90deg, it will result in the same results as prior
            // and dec lines will overlap each other

            var (raStep, decStep, raStart, raStop, decStart, decStop) = CalculateStepsAndStartStopValues(viewport);

            var pointsByDecDict = new Dictionary<double, FrameLine>();

            // if raStep is 30 and decStep is 1 just
            bool raIsClosed = RoundToHigherValue(viewport.AbsCalcTopDec, decStep) >= MAXDEC;

            for (double ra = Math.Min(raStart, raStop);
                ra <= Math.Max(raStop, raStart);
                ra += raStep) {
                List<PointF> raPointCollection = new List<PointF>();

                for (double dec = Math.Min(decStart, decStop);
                    dec <= Math.Max(decStart, decStop);
                    dec += decStep) {
                    var point = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees).XYProjection(viewport);
                    var pointf = new PointF((float)point.X, (float)point.Y);
                    if (!pointsByDecDict.ContainsKey(dec)) {
                        pointsByDecDict.Add(dec, new FrameLine() { Closed = raIsClosed, Collection = new List<PointF> { pointf }, StrokeThickness = dec == 0 ? 3 : 1 });
                    } else {
                        pointsByDecDict[dec].Collection.Add(pointf);
                    }

                    raPointCollection.Add(pointf);
                }

                // those are the vertical lines
                RAPoints.Add(new FrameLine {
                    StrokeThickness = (ra == 0 || ra == 180) ? 3 : 1,
                    Closed = false,
                    Collection = raPointCollection
                });
            }

            // those are actually the circles
            foreach (KeyValuePair<double, FrameLine> item in pointsByDecDict) {
                DecPoints.Add(item.Value);
            }
        }

        private (double raStep, double decStep, double raStart, double raStop, double decStart, double decStop) CalculateStepsAndStartStopValues(ViewportFoV viewport) {
            double raStart;
            double raStop;
            double decStart;
            double decStop;
            double raStep;
            double decStep;
            var realTopDec = viewport.AbsCalcTopDec;
            var realBottomDec = viewport.AbsCalcBottomDec;

            if (realTopDec >= MAXDEC) {
                realTopDec = MAXDEC;
                raStep = 30; // 12 lines at top
                decStep = 1;

                raStart = 0;
                raStop = 360 - raStep;
            } else {
                // we want at least 4 lines
                // get the steps that are closest to vFovDegTotal and closest to hFovDeg
                decStep = viewport.VFoVDeg / 4;
                decStep = DECSTEPS.Aggregate((x, y) => Math.Abs(x - decStep) < Math.Abs(y - decStep) ? x : y);

                raStep = viewport.HFoVDeg / 4;
                raStep = RASTEPS.Aggregate((x, y) => Math.Abs(x - raStep) < Math.Abs(y - raStep) ? x : y);

                // avoid "crash" using a higher fov and getting close to dec, so it doesn't move from e.g. 16 to 1 instantly but "smoothly"
                if (realTopDec + decStep > MAXDEC) {
                    do {
                        if (decStep == 1) {
                            realTopDec = MAXDEC;
                            raStep = 30;
                            break;
                        }

                        decStep = DECSTEPS[Array.FindIndex(DECSTEPS, w => w == decStep) - 1];
                    } while (realTopDec + decStep > MAXDEC);
                }

                if (raStep != 30) {
                    // round properly so all ra lines are always visible and not cut off unless raStep is 30
                    raStop = viewport.TopLeft.RADegrees - viewport.HFoVDeg < 0
                        ? RoundToHigherValue(viewport.TopLeft.RADegrees - viewport.HFoVDeg, raStep)
                        : RoundToLowerValue(viewport.TopLeft.RADegrees - viewport.HFoVDeg, raStep);
                    raStart = RoundToHigherValue(viewport.TopLeft.RADegrees, raStep);
                } else {
                    raStart = 0;
                    raStop = 360 - raStep;
                }
            }

            // if the top declination point is at max declination we have to round the lower declination point up so it doesn't get cut off
            if (realTopDec == MAXDEC) {
                decStart = RoundToHigherValue(realBottomDec, decStep);
            } else {
                // otherwise round according to the location of the dec to avoid cutting off
                decStart = realBottomDec < 0
                    ? RoundToHigherValue(realBottomDec, decStep)
                    : RoundToLowerValue(realBottomDec, decStep);
            }

            // we want to round decstop always higher
            decStop = RoundToHigherValue(realTopDec, decStep);
            if (decStop >= MAXDEC) {
                decStop = MAXDEC;
            }

            // flip coordinates if necessary
            decStart *= viewport.AboveZero ? 1 : -1;
            decStop *= viewport.AboveZero ? 1 : -1;
            return (raStep, decStep, raStart, raStop, decStart, decStop);
        }

        public List<FrameLine> RAPoints { get; private set; }

        public List<FrameLine> DecPoints { get; private set; }

        public static double RoundToHigherValue(double value, double multiple) {
            return (Math.Abs(value) + (multiple - Math.Abs(value) % multiple)) * Math.Sign(value);
        }

        public static double RoundToLowerValue(double value, double multiple) {
            return (Math.Abs(value) - (Math.Abs(value) % multiple)) * Math.Sign(value);
        }

        private static System.Drawing.Pen gridPen = new System.Drawing.Pen(System.Drawing.Color.SteelBlue);

        public void Draw(Graphics g) {
            foreach (var frameLine in this.RAPoints) {
                DrawFrameLineCollection(g, frameLine);
            }

            foreach (var frameLine in this.DecPoints) {
                DrawFrameLineCollection(g, frameLine);
            }
        }

        private void DrawFrameLineCollection(Graphics g, FrameLine frameLine) {
            if (frameLine.Collection.Count > 1) {
                var points = CardinalSpline(frameLine.Collection, 0.5f, frameLine.Closed);

                if (frameLine.StrokeThickness != 1) {
                    using (var pen = new System.Drawing.Pen(gridPen.Color, frameLine.StrokeThickness)) {
                        g.DrawBeziers(pen, points.ToArray());
                    }
                } else {
                    g.DrawBeziers(gridPen, points.ToArray());
                }
            }
        }

        private static void CalcCurve(PointF[] pts, float tension, out PointF p1, out PointF p2) {
            float deltaX, deltaY;
            deltaX = pts[2].X - pts[0].X;
            deltaY = pts[2].Y - pts[0].Y;
            p1 = new PointF((pts[1].X - tension * deltaX), (pts[1].Y - tension * deltaY));
            p2 = new PointF((pts[1].X + tension * deltaX), (pts[1].Y + tension * deltaY));
        }

        private void CalcCurveEnd(PointF end, PointF adj, float tension, out PointF p1) {
            p1 = new PointF(((tension * (adj.X - end.X) + end.X)), ((tension * (adj.Y - end.Y) + end.Y)));
        }

        private List<PointF> CardinalSpline(List<PointF> pts, float t, bool closed) {
            int i, nrRetPts;
            PointF p1, p2;
            float tension = t * (1f / 3f); //we are calculating contolpoints.

            if (closed) {
                nrRetPts = (pts.Count + 1) * 3 - 2;
            } else {
                nrRetPts = pts.Count * 3 - 2;
            }

            PointF[] retPnt = new PointF[nrRetPts];
            for (i = 0; i < nrRetPts; i++) {
                retPnt[i] = new PointF();
            }

            if (!closed) {
                CalcCurveEnd(pts[0], pts[1], tension, out p1);
                retPnt[0] = pts[0];
                retPnt[1] = p1;
            }
            for (i = 0; i < pts.Count - (closed ? 1 : 2); i++) {
                CalcCurve(new PointF[] { pts[i], pts[i + 1], pts[(i + 2) % pts.Count] }, tension, out p1, out p2);
                retPnt[3 * i + 2] = p1;
                retPnt[3 * i + 3] = pts[i + 1];
                retPnt[3 * i + 4] = p2;
            }
            if (closed) {
                CalcCurve(new PointF[] { pts[pts.Count - 1], pts[0], pts[1] }, tension, out p1, out p2);
                retPnt[nrRetPts - 2] = p1;
                retPnt[0] = pts[0];
                retPnt[1] = p2;
                retPnt[nrRetPts - 1] = retPnt[0];
            } else {
                CalcCurveEnd(pts[pts.Count - 1], pts[pts.Count - 2], tension, out p1);
                retPnt[nrRetPts - 2] = p1;
                retPnt[nrRetPts - 1] = pts[pts.Count - 1];
            }
            return new List<PointF>(retPnt);
        }
    }
}