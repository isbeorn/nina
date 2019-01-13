using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using FontStyle = System.Drawing.FontStyle;
using Pen = System.Drawing.Pen;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace NINA.ViewModel.FramingAssistant {

    internal class SkyMapAnnotator : BaseINPC {
        private readonly DatabaseInteraction dbInstance;
        private ViewportFoV viewportFoV;
        private List<Constellation> dbConstellations;
        private Dictionary<string, DeepSkyObject> dbDSOs;
        private Bitmap img;
        private Graphics g;

        public SkyMapAnnotator(string databaseLocation) {
            dbInstance = new DatabaseInteraction(databaseLocation);
            DSOInViewport = new List<FramingDSO>();
            ConstellationsInViewport = new List<FramingConstellation>();
            FrameLineMatrix = new FrameLineMatrix();
            ConstellationBoundaries = new Dictionary<string, ConstellationBoundary>();
        }

        public async Task Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CancellationToken ct) {
            AnnotateDSO = true;
            AnnotateGrid = true;

            viewportFoV = new ViewportFoV(centerCoordinates, vFoVDegrees, imageWidth, imageHeight, imageRotation);

            dbConstellations = await dbInstance.GetConstellationsWithStars(ct);

            using (MyStopWatch.Measure()) {
                var param = new DatabaseInteraction.DeepSkyObjectSearchParams();
                // calculate size, at 10deg fov we want all items, at 45deg fov only the items that are larger than 100
                // basic linear regression (:calculus:)
                var minSize = (2.857 * viewportFoV.OriginalVFoV - 28.57);
                var maxSize = Astrometry.DegreeToArcsec(2 * Math.Max(viewportFoV.OriginalHFoV, viewportFoV.OriginalVFoV));

                param.Size = new DatabaseInteraction.DeepSkyObjectSearchFromThru<string> {
                    From = Math.Max(0, minSize).ToString(CultureInfo.InvariantCulture),
                    Thru = maxSize.ToString(CultureInfo.InvariantCulture)
                };

                dbDSOs = (await dbInstance.GetDeepSkyObjects(string.Empty, param, ct)).ToDictionary(x => x.Id, y => y);
            }

            ConstellationsInViewport.Clear();
            ClearFrameLineMatrix();

            img = new Bitmap((int)viewportFoV.OriginalWidth, (int)viewportFoV.OriginalHeight, PixelFormat.Format32bppArgb);

            g = Graphics.FromImage(img);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            FrameLineMatrix.CalculatePoints(viewportFoV);
            if (ConstellationBoundaries.Count == 0) {
                ConstellationBoundaries = await GetConstellationBoundaries();
            }

            UpdateSkyMap();
        }

        public FrameLineMatrix FrameLineMatrix { get; private set; }

        public List<FramingDSO> DSOInViewport { get; private set; }

        public List<FramingConstellation> ConstellationsInViewport { get; private set; }

        private bool annotateConstellationBoundaries;

        public bool AnnotateConstellationBoundaries {
            get => annotateConstellationBoundaries;
            set {
                annotateConstellationBoundaries = value;
                RaisePropertyChanged();
            }
        }

        private bool annotateConstellations;

        public bool AnnotateConstellations {
            get => annotateConstellations;
            set {
                annotateConstellations = value;
                RaisePropertyChanged();
            }
        }

        private bool annotateGrid;

        public bool AnnotateGrid {
            get => annotateGrid;
            set {
                annotateGrid = value;
                RaisePropertyChanged();
            }
        }

        private bool annotateDSO;

        public bool AnnotateDSO {
            get => annotateDSO;
            set {
                annotateDSO = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Query for skyobjects for a reference coordinate that overlap the current viewport
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DeepSkyObject> GetDeepSkyObjectsForViewport() {
            var dsoList = new Dictionary<string, DeepSkyObject>();

            // if we're above 90deg centerTop will be different than centerBottom, otherwise it is equal
            if (viewportFoV.IsAbove90) {
                dsoList = dbDSOs.Where(x =>
                    x.Value.Coordinates.Dec > (!viewportFoV.AboveZero ? -90 : viewportFoV.BottomLeft.Dec)
                    && x.Value.Coordinates.Dec < (!viewportFoV.AboveZero ? viewportFoV.BottomLeft.Dec : 90)
                ).ToDictionary(x => x.Key, y => y.Value);
            } else {
                var raFrom = viewportFoV.TopLeft.RADegrees - viewportFoV.HFoVDeg;
                var raThru = viewportFoV.TopLeft.RADegrees;
                if (raFrom < 0) {
                    dsoList = dbDSOs.Where(x =>
                        (x.Value.Coordinates.RADegrees > 360 + raFrom || x.Value.Coordinates.RADegrees < raThru)
                        && x.Value.Coordinates.Dec > Math.Min(viewportFoV.TopCenter.Dec, viewportFoV.BottomLeft.Dec)
                        && x.Value.Coordinates.Dec < Math.Max(viewportFoV.BottomLeft.Dec, viewportFoV.TopCenter.Dec)
                    ).ToDictionary(x => x.Key, y => y.Value); ;
                } else {
                    dsoList = dbDSOs.Where(x =>
                        x.Value.Coordinates.RADegrees > (viewportFoV.TopLeft.RADegrees - viewportFoV.HFoVDeg)
                        && x.Value.Coordinates.RADegrees < (viewportFoV.TopLeft.RADegrees)
                        && x.Value.Coordinates.Dec > Math.Min(viewportFoV.TopCenter.Dec, viewportFoV.BottomLeft.Dec)
                        && x.Value.Coordinates.Dec < Math.Max(viewportFoV.BottomLeft.Dec, viewportFoV.TopCenter.Dec)
                    ).ToDictionary(x => x.Key, y => y.Value); ;
                }
            }
            return dsoList;
        }

        public Coordinates ShiftViewport(Vector delta) {
            viewportFoV.Shift(delta);

            return viewportFoV.CenterCoordinates;
        }

        public void ClearFrameLineMatrix() {
            FrameLineMatrix.RAPoints.Clear();
            FrameLineMatrix.DecPoints.Clear();
        }

        public void CalculateFrameLineMatrix() {
            FrameLineMatrix.CalculatePoints(viewportFoV);
        }

        private Dictionary<string, ConstellationBoundary> ConstellationBoundaries;
        private BitmapSource skyMapOverlay;

        private async Task<Dictionary<string, ConstellationBoundary>> GetConstellationBoundaries() {
            var dic = new Dictionary<string, ConstellationBoundary>();
            var list = await dbInstance.GetConstellationBoundaries(new CancellationToken());
            foreach (var item in list) {
                dic.Add(item.Name, item);
            }
            return dic;
        }

        public List<FrameConstellation> ConstellationBoundariesInViewPort { get; private set; } = new List<FrameConstellation>();

        public void CalculateConstellationBoundaries() {
            ConstellationBoundariesInViewPort.Clear();
            foreach (var boundary in ConstellationBoundaries) {
                var frameLine = new FrameConstellation();
                bool isInViewport = false;
                foreach (var coordinates in boundary.Value.Boundaries) {
                    isInViewport = viewportFoV.ContainsCoordinates(coordinates);
                    if (isInViewport) {
                        break;
                    }
                }

                if (!isInViewport) {
                    continue;
                }

                foreach (var coordinates in boundary.Value.Boundaries) {
                    var point = coordinates.GnomonicTanProjection(viewportFoV);
                    if (viewportFoV.IsOutOfViewportBounds(point)) {
                        continue;
                    }

                    frameLine.Points.Add(new PointF((float)point.X, (float)point.Y));
                }

                ConstellationBoundariesInViewPort.Add(frameLine);
            }
        }

        private void RedrawDSOs() {
            var allGatheredDSO = GetDeepSkyObjectsForViewport();

            var existingDSOs = new List<string>();
            for (int i = DSOInViewport.Count - 1; i >= 0; i--) {
                var dso = DSOInViewport[i];
                if (allGatheredDSO.ContainsKey(dso.Id)) {
                    dso.RecalculateTopLeft(viewportFoV);
                    existingDSOs.Add(dso.Id);
                } else {
                    DSOInViewport.RemoveAt(i);
                }
            }

            var dsosToAdd = allGatheredDSO.Where(x => !existingDSOs.Any(y => y == x.Value.Id));
            foreach (var dso in dsosToAdd) {
                DSOInViewport.Add(new FramingDSO(dso.Value, viewportFoV));
            }

            foreach (var dso in DSOInViewport) {
                g.FillEllipse(dsoFillColorBrush, (float)(dso.CenterPoint.X - dso.RadiusWidth), (float)(dso.CenterPoint.Y - dso.RadiusHeight),
                    (float)(dso.RadiusWidth * 2), (float)(dso.RadiusHeight * 2));
                g.DrawEllipse(dsoStrokePen, (float)(dso.CenterPoint.X - dso.RadiusWidth), (float)(dso.CenterPoint.Y - dso.RadiusHeight),
                    (float)(dso.RadiusWidth * 2), (float)(dso.RadiusHeight * 2));
                var size1 = g.MeasureString(dso.Name1, fontdso);
                g.DrawString(dso.Name1, fontdso, dsoFontColorBrush, (float)(dso.TextPosition.X - size1.Width / 2), (float)(dso.TextPosition.Y));
                if (dso.Name2 != null) {
                    var size2 = g.MeasureString(dso.Name2, fontdso);
                    g.DrawString(dso.Name2, fontdso, dsoFontColorBrush, (float)(dso.TextPosition.X - size2.Width / 2), (float)(dso.TextPosition.Y + size1.Height + 2));
                    if (dso.Name3 != null) {
                        var size3 = g.MeasureString(dso.Name3, fontdso);
                        g.DrawString(dso.Name3, fontdso, dsoFontColorBrush, (float)(dso.TextPosition.X - size3.Width / 2), (float)(dso.TextPosition.Y + size1.Height + 2 + size2.Height + 2));
                    }
                }
            }
        }

        private void RedrawConstellations() {
            foreach (var constellation in dbConstellations) {
                var viewPortConstellation = ConstellationsInViewport.FirstOrDefault(x => x.Id == constellation.Id);

                var isInViewport = false;
                foreach (var star in constellation.Stars) {
                    if (!viewportFoV.ContainsCoordinates(star.Coords)) {
                        continue;
                    }

                    isInViewport = true;
                    break;
                }

                if (isInViewport) {
                    if (viewPortConstellation == null) {
                        ConstellationsInViewport.Add(new FramingConstellation(constellation, viewportFoV));
                    } else {
                        viewPortConstellation.RecalculateConstellationPoints(viewportFoV);
                    }
                } else if (viewPortConstellation != null) {
                    ConstellationsInViewport.Remove(viewPortConstellation);
                }
            }

            foreach (var constellation in ConstellationsInViewport) {
                var constellationSize = g.MeasureString(constellation.Name, fontconst);
                g.DrawString(constellation.Name, fontconst, constColorBrush, (float)(constellation.CenterPoint.X - constellationSize.Width / 2), (float)(constellation.CenterPoint.Y));
                foreach (var starConnection in constellation.Points) {
                    g.DrawLine(constLinePen, (float)starConnection.Item1.Position.X,
                    (float)starConnection.Item1.Position.Y, (float)starConnection.Item2.Position.X,
                    (float)starConnection.Item2.Position.Y);
                }

                foreach (var star in constellation.Stars) {
                    g.DrawEllipse(starPen, (float)(star.Position.X - star.Radius), (float)(star.Position.Y - star.Radius), (float)star.Radius * 2, (float)star.Radius * 2);
                    var size = g.MeasureString(star.Name, font);
                    g.DrawString(star.Name, font, starFontColorBrush, (float)(star.Position.X + star.Radius - size.Width / 2), (float)(star.Position.Y + star.Radius * 2 + 5));
                }
            }
        }

        private void RedrawConstellationBoundaries() {
            CalculateConstellationBoundaries();
            foreach (var constellationBoundary in ConstellationBoundariesInViewPort) {
                if (constellationBoundary.Points.Count > 1) {
                    g.DrawPolygon(boundaryPen, constellationBoundary.Points.ToArray());
                }
            }
        }

        private void RedrawGrid() {
            ClearFrameLineMatrix();
            CalculateFrameLineMatrix();

            foreach (var frameLine in FrameLineMatrix.RAPoints) {
                var points = cardinalSpline(frameLine.Collection, 0.5f, frameLine.Closed);
                if (frameLine.StrokeThickness != 1) {
                    var pen = new Pen(gridPen.Color, frameLine.StrokeThickness);
                    g.DrawBeziers(pen, points.ToArray());
                } else {
                    g.DrawBeziers(gridPen, points.ToArray());
                }
            }

            foreach (var frameLine in FrameLineMatrix.DecPoints) {
                var points = cardinalSpline(frameLine.Collection, 0.5f, frameLine.Closed);

                if (frameLine.StrokeThickness != 1) {
                    var pen = new Pen(gridPen.Color, frameLine.StrokeThickness);
                    g.DrawBeziers(pen, points.ToArray());
                } else {
                    g.DrawBeziers(gridPen, points.ToArray());
                }
            }
        }

        private static void CalcCurve(PointF[] pts, float tenstion, out PointF p1, out PointF p2) {
            float deltaX, deltaY;
            deltaX = pts[2].X - pts[0].X;
            deltaY = pts[2].Y - pts[0].Y;
            p1 = new PointF((pts[1].X - tenstion * deltaX), (pts[1].Y - tenstion * deltaY));
            p2 = new PointF((pts[1].X + tenstion * deltaX), (pts[1].Y + tenstion * deltaY));
        }

        private void CalcCurveEnd(PointF end, PointF adj, float tension, out PointF p1) {
            p1 = new PointF(((tension * (adj.X - end.X) + end.X)), ((tension * (adj.Y - end.Y) + end.Y)));
        }

        private List<PointF> cardinalSpline(List<PointF> pts, float t, bool closed) {
            int i, nrRetPts;
            PointF p1, p2;
            float tension = t * (1f / 3f); //we are calculating contolpoints.

            if (closed)
                nrRetPts = (pts.Count + 1) * 3 - 2;
            else
                nrRetPts = pts.Count * 3 - 2;

            PointF[] retPnt = new PointF[nrRetPts];
            for (i = 0; i < nrRetPts; i++)
                retPnt[i] = new PointF();

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

        public void UpdateSkyMap() {
            g.Clear(Color.Transparent);

            if (AnnotateDSO) {
                RedrawDSOs();
            }

            if (AnnotateConstellations) {
                RedrawConstellations();
            }

            if (AnnotateConstellationBoundaries) {
                RedrawConstellationBoundaries();
            }

            if (AnnotateGrid) {
                RedrawGrid();
            }

            var source = ImageAnalysis.ConvertBitmap(img, PixelFormats.Bgra32);
            source.Freeze();
            SkyMapOverlay = source;
        }

        private Font font = new Font("Segoe UI", 8, FontStyle.Italic);
        private Font fontconst = new Font("Segoe UI", 11, FontStyle.Bold);
        private Font fontdso = new Font("Segoe UI", 10, FontStyle.Regular);

        private SolidBrush constColorBrush = new SolidBrush(Color.FromArgb(128, 255, 255, 153));
        private SolidBrush starFontColorBrush = new SolidBrush(Color.FromArgb(128, 255, 215, 0));
        private SolidBrush dsoFillColorBrush = new SolidBrush(Color.FromArgb(10, 255, 255, 255));
        private SolidBrush dsoFontColorBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

        private Pen constLinePen = new Pen(Color.FromArgb(128, 0, 255, 0));
        private Pen starPen = new Pen(Color.FromArgb(128, 255, 255, 255));
        private Pen dsoStrokePen = new Pen(Color.FromArgb(255, 255, 255, 255));
        private Pen gridPen = new Pen(Color.SteelBlue);
        private Pen boundaryPen = new Pen(Color.Yellow);

        public BitmapSource SkyMapOverlay {
            get => skyMapOverlay;
            set {
                skyMapOverlay = value;
                RaisePropertyChanged();
            }
        }
    }
}