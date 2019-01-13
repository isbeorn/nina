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
            ConstellationBoundaries = new AsyncLazy<Dictionary<string, ConstellationBoundary>>(async delegate {
                return await GetConstellationBoundaries();
            });
        }

        public async Task Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CancellationToken ct) {
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
            ClearConstellationBoundaries();

            img = new Bitmap((int)viewportFoV.OriginalWidth, (int)viewportFoV.OriginalHeight, PixelFormat.Format32bppArgb);

            g = Graphics.FromImage(img);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            UpdateSkyMap();

            FrameLineMatrix.CalculatePoints(viewportFoV);
            await CalculateConstellationBoundaries();
        }

        public FrameLineMatrix FrameLineMatrix { get; private set; }

        public List<FramingDSO> DSOInViewport { get; private set; }

        public List<FramingConstellation> ConstellationsInViewport { get; private set; }

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

        private AsyncLazy<Dictionary<string, ConstellationBoundary>> ConstellationBoundaries;
        private BitmapSource skyMapOverlay;

        private async Task<Dictionary<string, ConstellationBoundary>> GetConstellationBoundaries() {
            var dic = new Dictionary<string, ConstellationBoundary>();
            var list = await dbInstance.GetConstellationBoundaries(new CancellationToken());
            foreach (var item in list) {
                dic.Add(item.Name, item);
            }
            return dic;
        }

        public AsyncObservableCollection<FrameLine> ConstellationBoundariesInViewPort { get; private set; } = new AsyncObservableCollection<FrameLine>();

        public void ClearConstellationBoundaries() {
            ConstellationBoundariesInViewPort.Clear();
        }

        public async Task CalculateConstellationBoundaries() {
            foreach (var boundary in await ConstellationBoundaries) {
                var frameLine = new FrameLine() { Closed = false, StrokeThickness = 0.5, Collection = new System.Windows.Media.PointCollection() };
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

                    frameLine.Collection.Add(point);
                }

                ConstellationBoundariesInViewPort.Add(frameLine);
            }
        }

        public void UpdateSkyMap() {
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

            var dsosToAdd = allGatheredDSO.Where(x => !existingDSOs.Any(y => y == x.Value.Id));
            foreach (var dso in dsosToAdd) {
                DSOInViewport.Add(new FramingDSO(dso.Value, viewportFoV));
            }
            Draw();
        }

        private void Draw() {
            g.Clear(Color.Transparent);

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

            var source = ImageAnalysis.ConvertBitmap(img, PixelFormats.Bgra32);
            source.Freeze();
            SkyMapOverlay = source;
        }

        private Font font = new Font("Segoe UI", 8, FontStyle.Italic);
        private Font fontconst = new Font("Segoe UI", 11, FontStyle.Bold);
        private Font fontdso = new Font("Segoe UI", 10, FontStyle.Regular);

        private Pen constLinePen = new Pen(Color.FromArgb(128, 0, 255, 0));
        private SolidBrush constColorBrush = new SolidBrush(Color.FromArgb(128, 255, 255, 153));
        private Pen starPen = new Pen(Color.FromArgb(128, 255, 255, 255));
        private SolidBrush starFontColorBrush = new SolidBrush(Color.FromArgb(128, 255, 215, 0));
        private SolidBrush dsoFillColorBrush = new SolidBrush(Color.FromArgb(10, 255, 255, 255));
        private Pen dsoStrokePen = new Pen(Color.FromArgb(255, 255, 255, 255));
        private SolidBrush dsoFontColorBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

        public BitmapSource SkyMapOverlay {
            get => skyMapOverlay;
            set {
                skyMapOverlay = value;
                RaisePropertyChanged();
            }
        }
    }
}