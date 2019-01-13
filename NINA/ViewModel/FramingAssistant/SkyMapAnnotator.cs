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
        public ViewportFoV ViewportFoV { get; private set; }
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

            ViewportFoV = new ViewportFoV(centerCoordinates, vFoVDegrees, imageWidth, imageHeight, imageRotation);

            if (dbConstellations == null) {
                dbConstellations = await dbInstance.GetConstellationsWithStars(ct);
            }

            if (dbDSOs == null) {
                dbDSOs = (await dbInstance.GetDeepSkyObjects(string.Empty, new DatabaseInteraction.DeepSkyObjectSearchParams(), ct)).ToDictionary(x => x.Id, y => y);
            }

            ConstellationsInViewport.Clear();
            ClearFrameLineMatrix();

            img = new Bitmap((int)ViewportFoV.OriginalWidth, (int)ViewportFoV.OriginalHeight, PixelFormat.Format32bppArgb);

            g = Graphics.FromImage(img);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            FrameLineMatrix.CalculatePoints(ViewportFoV);
            if (ConstellationBoundaries.Count == 0) {
                ConstellationBoundaries = await GetConstellationBoundaries();
            }

            UpdateSkyMap();
        }

        public ViewportFoV ChangeFoV(double vFoVDegrees) {
            ConstellationsInViewport.Clear();
            ClearFrameLineMatrix();
            ViewportFoV = new ViewportFoV(ViewportFoV.CenterCoordinates, vFoVDegrees, ViewportFoV.OriginalWidth, ViewportFoV.OriginalHeight, ViewportFoV.Rotation);

            FrameLineMatrix.CalculatePoints(ViewportFoV);

            UpdateSkyMap();

            return ViewportFoV;
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

        private bool dynamicFoV;

        public bool DynamicFoV {
            get {
                return dynamicFoV;
            }
            set {
                dynamicFoV = value;
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

            var minSize = (2.857 * ViewportFoV.OriginalVFoV - 28.57);
            var maxSize = Astrometry.DegreeToArcsec(2 * Math.Max(ViewportFoV.OriginalHFoV, ViewportFoV.OriginalVFoV));

            var filteredDbDSO = dbDSOs.Where(d => (d.Value.Size != null && d.Value.Size > minSize && d.Value.Size < maxSize) || ViewportFoV.VFoVDeg <= 10).ToList();

            // if we're above 90deg centerTop will be different than centerBottom, otherwise it is equal
            if (ViewportFoV.IsAbove90) {
                dsoList = filteredDbDSO.Where(x =>
                    x.Value.Coordinates.Dec > (!ViewportFoV.AboveZero ? -90 : ViewportFoV.BottomLeft.Dec)
                    && x.Value.Coordinates.Dec < (!ViewportFoV.AboveZero ? ViewportFoV.BottomLeft.Dec : 90)
                ).ToDictionary(x => x.Key, y => y.Value);
            } else {
                var raFrom = ViewportFoV.TopLeft.RADegrees - ViewportFoV.HFoVDeg;
                var raThru = ViewportFoV.TopLeft.RADegrees;
                if (raFrom < 0) {
                    dsoList = filteredDbDSO.Where(x =>
                        (x.Value.Coordinates.RADegrees > 360 + raFrom || x.Value.Coordinates.RADegrees < raThru)
                        && x.Value.Coordinates.Dec > Math.Min(ViewportFoV.TopCenter.Dec, ViewportFoV.BottomLeft.Dec)
                        && x.Value.Coordinates.Dec < Math.Max(ViewportFoV.BottomLeft.Dec, ViewportFoV.TopCenter.Dec)
                    ).ToDictionary(x => x.Key, y => y.Value); ;
                } else {
                    dsoList = filteredDbDSO.Where(x =>
                        x.Value.Coordinates.RADegrees > (ViewportFoV.TopLeft.RADegrees - ViewportFoV.HFoVDeg)
                        && x.Value.Coordinates.RADegrees < (ViewportFoV.TopLeft.RADegrees)
                        && x.Value.Coordinates.Dec > Math.Min(ViewportFoV.TopCenter.Dec, ViewportFoV.BottomLeft.Dec)
                        && x.Value.Coordinates.Dec < Math.Max(ViewportFoV.BottomLeft.Dec, ViewportFoV.TopCenter.Dec)
                    ).ToDictionary(x => x.Key, y => y.Value); ;
                }
            }
            return dsoList;
        }

        public Coordinates ShiftViewport(Vector delta) {
            ViewportFoV.Shift(delta);

            return ViewportFoV.CenterCoordinates;
        }

        public void ClearFrameLineMatrix() {
            FrameLineMatrix.RAPoints.Clear();
            FrameLineMatrix.DecPoints.Clear();
        }

        public void CalculateFrameLineMatrix() {
            FrameLineMatrix.CalculatePoints(ViewportFoV);
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

        public List<FramingConstellationBoundary> ConstellationBoundariesInViewPort { get; private set; } = new List<FramingConstellationBoundary>();

        public void CalculateConstellationBoundaries() {
            ConstellationBoundariesInViewPort.Clear();
            foreach (var boundary in ConstellationBoundaries) {
                var frameLine = new FramingConstellationBoundary();
                bool isInViewport = false;
                foreach (var coordinates in boundary.Value.Boundaries) {
                    isInViewport = ViewportFoV.ContainsCoordinates(coordinates);
                    if (isInViewport) {
                        break;
                    }
                }

                if (!isInViewport) {
                    continue;
                }

                foreach (var coordinates in boundary.Value.Boundaries) {
                    var point = coordinates.GnomonicTanProjection(ViewportFoV);
                    if (ViewportFoV.IsOutOfViewportBounds(point)) {
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
                    dso.RecalculateTopLeft(ViewportFoV);
                    existingDSOs.Add(dso.Id);
                } else {
                    DSOInViewport.RemoveAt(i);
                }
            }

            var dsosToAdd = allGatheredDSO.Where(x => !existingDSOs.Any(y => y == x.Value.Id));
            foreach (var dso in dsosToAdd) {
                DSOInViewport.Add(new FramingDSO(dso.Value, ViewportFoV));
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
                    if (!ViewportFoV.ContainsCoordinates(star.Coords)) {
                        continue;
                    }

                    isInViewport = true;
                    break;
                }

                if (isInViewport) {
                    if (viewPortConstellation == null) {
                        ConstellationsInViewport.Add(new FramingConstellation(constellation, ViewportFoV));
                    } else {
                        viewPortConstellation.RecalculateConstellationPoints(ViewportFoV);
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