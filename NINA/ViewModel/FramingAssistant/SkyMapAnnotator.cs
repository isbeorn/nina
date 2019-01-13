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

        private void UpdateDSOs() {
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
                dso.Draw(g);
            }
        }

        private void UpdateConstellations() {
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
                constellation.Draw(g);
            }
        }

        private void UpdateConstellationBoundaries() {
            CalculateConstellationBoundaries();
            foreach (var constellationBoundary in ConstellationBoundariesInViewPort) {
                constellationBoundary.Draw(g);
            }
        }

        private void UpdateGrid() {
            ClearFrameLineMatrix();
            CalculateFrameLineMatrix();

            FrameLineMatrix.Draw(g);
        }

        public void UpdateSkyMap() {
            g.Clear(Color.Transparent);

            if (AnnotateDSO) {
                UpdateDSOs();
            }

            if (AnnotateConstellations) {
                UpdateConstellations();
            }

            if (AnnotateConstellationBoundaries) {
                UpdateConstellationBoundaries();
            }

            if (AnnotateGrid) {
                UpdateGrid();
            }

            var source = ImageAnalysis.ConvertBitmap(img, PixelFormats.Bgra32);
            source.Freeze();
            SkyMapOverlay = source;
        }

        public BitmapSource SkyMapOverlay {
            get => skyMapOverlay;
            set {
                skyMapOverlay = value;
                RaisePropertyChanged();
            }
        }
    }
}