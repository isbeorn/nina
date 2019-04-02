using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace NINA.ViewModel.FramingAssistant {

    internal class SkyMapAnnotator : BaseINPC, ITelescopeConsumer {
        private readonly DatabaseInteraction dbInstance;
        public ViewportFoV ViewportFoV { get; private set; }
        private List<Constellation> dbConstellations;
        private Dictionary<string, DeepSkyObject> dbDSOs;
        private Bitmap img;
        private Graphics g;
        private ITelescopeMediator telescopeMediator;

        public SkyMapAnnotator(string databaseLocation, ITelescopeMediator mediator) {
            this.telescopeMediator = mediator;
            dbInstance = new DatabaseInteraction(databaseLocation);
            DSOInViewport = new List<FramingDSO>();
            ConstellationsInViewport = new List<FramingConstellation>();
            FrameLineMatrix = new FrameLineMatrix2();
            ConstellationBoundaries = new Dictionary<string, ConstellationBoundary>();
        }

        public async Task Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CancellationToken ct) {
            telescopeMediator.RemoveConsumer(this);

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
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            FrameLineMatrix.CalculatePoints(ViewportFoV);
            if (ConstellationBoundaries.Count == 0) {
                ConstellationBoundaries = await GetConstellationBoundaries();
            }

            telescopeMediator.RegisterConsumer(this);
            Initialized = true;

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

        public ICommand DragCommand { get; private set; }

        public FrameLineMatrix2 FrameLineMatrix { get; private set; }

        public List<FramingDSO> DSOInViewport { get; private set; }

        public bool Initialized { get; private set; }

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

            double minSize = 0;
            if (!(Math.Min(ViewportFoV.OriginalHFoV, ViewportFoV.OriginalVFoV) < 10)) {
                // Stuff has to be at least 3 pixel wide
                minSize = 3 * Math.Min(ViewportFoV.ArcSecWidth, ViewportFoV.ArcSecHeight);
            }
            var maxSize = Astrometry.DegreeToArcsec(2 * Math.Max(ViewportFoV.OriginalHFoV, ViewportFoV.OriginalVFoV));

            var filteredDbDSO = dbDSOs.Where(d => (d.Value.Size != null && d.Value.Size > minSize && d.Value.Size < maxSize) || ViewportFoV.VFoVDeg <= 10).ToList();

            // if we're above 90deg centerTop will be different than centerBottom, otherwise it is equal
            if (ViewportFoV.IsAbove90) {
                dsoList = filteredDbDSO.Where(x =>
                    x.Value.Coordinates.Dec > (!ViewportFoV.AboveZero ? -90 : ViewportFoV.CenterCoordinates.Dec - ViewportFoV.VFoVDegBottom)
                    && x.Value.Coordinates.Dec < (!ViewportFoV.AboveZero ? ViewportFoV.CenterCoordinates.Dec + ViewportFoV.VFoVDegBottom : 90)
                ).ToDictionary(x => x.Key, y => y.Value);
            } else {
                var raFrom = ViewportFoV.TopLeft.RADegrees - ViewportFoV.HFoVDeg;
                var raThru = ViewportFoV.TopLeft.RADegrees;
                if (raFrom < 0) {
                    dsoList = filteredDbDSO.Where(x =>
                        (x.Value.Coordinates.RADegrees > 360 + raFrom || x.Value.Coordinates.RADegrees < raThru)
                        && x.Value.Coordinates.Dec > ViewportFoV.CenterCoordinates.Dec - ViewportFoV.VFoVDegBottom
                        && x.Value.Coordinates.Dec < ViewportFoV.CenterCoordinates.Dec + ViewportFoV.VFoVDegTop
                    ).ToDictionary(x => x.Key, y => y.Value);
                } else {
                    dsoList = filteredDbDSO.Where(x =>
                        x.Value.Coordinates.RADegrees > (ViewportFoV.TopLeft.RADegrees - ViewportFoV.HFoVDeg)
                        && x.Value.Coordinates.RADegrees < (ViewportFoV.TopLeft.RADegrees)
                        && x.Value.Coordinates.Dec > ViewportFoV.CenterCoordinates.Dec - ViewportFoV.VFoVDegBottom
                        && x.Value.Coordinates.Dec < ViewportFoV.CenterCoordinates.Dec + ViewportFoV.VFoVDegTop
                    ).ToDictionary(x => x.Key, y => y.Value);
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
                if (boundary.Value.Boundaries.Any((x) => ViewportFoV.ContainsCoordinates(x))) {
                    foreach (var coordinates in boundary.Value.Boundaries) {
                        var point = coordinates.XYProjection(ViewportFoV);
                        frameLine.Points.Add(new PointF((float)point.X, (float)point.Y));
                    }

                    ConstellationBoundariesInViewPort.Add(frameLine);
                }
            }
        }

        private void UpdateAndAnnotateDSOs() {
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

        private void DrawStars() {
            foreach (var constellation in ConstellationsInViewport) {
                constellation.DrawStars(g);
            }
        }

        private void UpdateAndAnnotateConstellations(bool drawAnnotations) {
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
                        var framingConstellation = new FramingConstellation(constellation, ViewportFoV);
                        framingConstellation.RecalculateConstellationPoints(ViewportFoV, drawAnnotations);
                        ConstellationsInViewport.Add(framingConstellation);
                    } else {
                        viewPortConstellation.RecalculateConstellationPoints(ViewportFoV, drawAnnotations);
                    }
                } else if (viewPortConstellation != null) {
                    ConstellationsInViewport.Remove(viewPortConstellation);
                }
            }

            if (drawAnnotations) {
                foreach (var constellation in ConstellationsInViewport) {
                    constellation.DrawAnnotations(g);
                }
            }
        }

        private void UpdateAndDrawConstellationBoundaries() {
            CalculateConstellationBoundaries();
            foreach (var constellationBoundary in ConstellationBoundariesInViewPort) {
                constellationBoundary.Draw(g);
            }
        }

        private void UpdateAndDrawGrid() {
            ClearFrameLineMatrix();
            CalculateFrameLineMatrix();

            FrameLineMatrix.Draw(g);
        }

        public void UpdateSkyMap() {
            if (Initialized) {
                g.Clear(Color.Transparent);

                if (!AnnotateConstellations && AnnotateDSO || AnnotateConstellations) {
                    UpdateAndAnnotateConstellations(AnnotateConstellations);
                    if (!AnnotateDSO) {
                        DrawStars();
                    }
                }

                if (AnnotateDSO) {
                    UpdateAndAnnotateDSOs();
                    DrawStars();
                }

                if (AnnotateConstellationBoundaries) {
                    UpdateAndDrawConstellationBoundaries();
                }

                if (AnnotateGrid) {
                    UpdateAndDrawGrid();
                }

                if (telescopeConnected) {
                    DrawTelescope();
                }

                var source = ImageAnalysis.ConvertBitmap(img, PixelFormats.Bgra32);
                source.Freeze();
                SkyMapOverlay = source;
            }
        }

        private void DrawTelescope() {
            if (ViewportFoV.ContainsCoordinates(telescopeCoordinates)) {
                System.Windows.Point scopePosition = telescopeCoordinates.XYProjection(ViewportFoV);
                g.DrawEllipse(ScopePen, (float)(scopePosition.X - 15), (float)(scopePosition.Y - 15), 30, 30);
                g.DrawLine(ScopePen, (float)(scopePosition.X), (float)(scopePosition.Y - 15),
                    (float)(scopePosition.X), (float)(scopePosition.Y - 5));
                g.DrawLine(ScopePen, (float)(scopePosition.X), (float)(scopePosition.Y + 5),
                    (float)(scopePosition.X), (float)(scopePosition.Y + 15));
                g.DrawLine(ScopePen, (float)(scopePosition.X - 15), (float)(scopePosition.Y),
                    (float)(scopePosition.X - 5), (float)(scopePosition.Y));
                g.DrawLine(ScopePen, (float)(scopePosition.X + 5), (float)(scopePosition.Y),
                    (float)(scopePosition.X + 15), (float)(scopePosition.Y));
            }
        }

        private static readonly Pen ScopePen = new Pen(Color.FromArgb(128, Color.Yellow), 2.0f);

        public BitmapSource SkyMapOverlay {
            get => skyMapOverlay;
            set {
                skyMapOverlay = value;
                RaisePropertyChanged();
            }
        }

        private bool telescopeConnected;
        private Coordinates telescopeCoordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            if (deviceInfo.Connected) {
                telescopeConnected = true;
                var coordinates = deviceInfo.Coordinates.Transform(Epoch.J2000);
                if (Math.Abs(telescopeCoordinates.RADegrees - coordinates.RADegrees) > 0.01 || Math.Abs(telescopeCoordinates.Dec - coordinates.Dec) > 0.01) {
                    telescopeCoordinates = coordinates;
                    UpdateSkyMap();
                }
            } else {
                telescopeConnected = false;
            }
        }
    }
}