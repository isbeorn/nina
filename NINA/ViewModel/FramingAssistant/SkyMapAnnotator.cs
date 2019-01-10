using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.FramingAssistant {
    internal class SkyMapAnnotator : BaseINPC {
        private readonly DatabaseInteraction dbInstance;
        private AsyncObservableCollection<FramingDSO> dsoInViewport;
        private ViewportFoV viewportFoV;
        private FrameLineMatrix frameLineMatrix;
        private AsyncObservableCollection<FramingConstellation> constellationsInViewport;
        private List<Constellation> dbConstellations;

        public SkyMapAnnotator(string databaseLocation) {
            dbInstance = new DatabaseInteraction(databaseLocation);
            DSOInViewport = new AsyncObservableCollection<FramingDSO>();
            ConstellationsInViewport = new AsyncObservableCollection<FramingConstellation>();
            FrameLineMatrix = new FrameLineMatrix();
            ConstellationBoundaries = new AsyncLazy<Dictionary<string, ConstellationBoundary>>(async delegate {
                return await GetConstellationBoundaries();
            });
        }

        public async Task Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CancellationToken ct) {
            viewportFoV = new ViewportFoV(centerCoordinates, vFoVDegrees, imageWidth, imageHeight, imageRotation);

            dbConstellations = await dbInstance.GetConstellationsWithStars(ct);

            ConstellationsInViewport.Clear();
            ClearFrameLineMatrix();
            ClearConstellationBoundaries();

            await UpdateSkyMap(ct);

            FrameLineMatrix.CalculatePoints(viewportFoV);
            await CalculateConstellationBoundaries();
        }

        public FrameLineMatrix FrameLineMatrix {
            get => frameLineMatrix;
            set {
                frameLineMatrix = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<FramingDSO> DSOInViewport {
            get => dsoInViewport;
            set {
                dsoInViewport = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<FramingConstellation> ConstellationsInViewport {
            get => constellationsInViewport;
            set {
                constellationsInViewport = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Query for skyobjects for a reference coordinate that overlap the current viewport
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<string, DeepSkyObject>> GetDeepSkyObjectsForViewport(CancellationToken ct) {
            Dictionary<string, DeepSkyObject> dsoList = new Dictionary<string, DeepSkyObject>();

            DatabaseInteraction.DeepSkyObjectSearchParams param =
                new DatabaseInteraction.DeepSkyObjectSearchParams();

            // calculate size, at 10deg fov we want all items, at 45deg fov only the items that are larger than 100
            // basic linear regression (:calculus:)
            var size = (2.857 * viewportFoV.OriginalVFoV - 28.57);
            if (size > 0) {
                param.Size = new DatabaseInteraction.DeepSkyObjectSearchFromThru<string> {
                    From = size.ToString(CultureInfo.InvariantCulture)
                };
            }

            // if we're above 90deg centerTop will be different than centerBottom, otherwise it is equal
            if (viewportFoV.IsAbove90) {
                // then we want everything from bottomLeft to 90 or -90 to bottomLeft (which is flipped when dec < 0 so it's actually "top" left)
                param.Declination = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = !viewportFoV.AboveZero ? -90 : viewportFoV.BottomLeft.Dec,
                    Thru = !viewportFoV.AboveZero ? viewportFoV.BottomLeft.Dec : 90
                };
                param.RightAscension = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = 0,
                    Thru = 360
                };
            } else {
                // depending on orientation we might be flipped so we search from lowest point to highest point
                param.Declination = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = Math.Min(viewportFoV.TopCenter.Dec, viewportFoV.BottomLeft.Dec),
                    Thru = Math.Max(viewportFoV.BottomLeft.Dec, viewportFoV.TopCenter.Dec)
                };
                // since topLeft.RADegrees is always higher than centerTop.RADegrees (counterclockwise circle) we can subtract hFovDeg to get the full RA
                param.RightAscension = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = viewportFoV.TopLeft.RADegrees - viewportFoV.HFoVDeg,
                    Thru = viewportFoV.TopLeft.RADegrees
                };
            }

            // if the calculated from RA is lower than zero we have to search from that point to 360
            // add the dso and then later search from 0 to the previous thru ra
            if (param.RightAscension.From < 0) {
                param.RightAscension = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = 360 + param.RightAscension.From,
                    Thru = 360
                };

                foreach (var dso in await dbInstance.GetDeepSkyObjects(
                    string.Empty, param, ct)) {
                    dsoList.Add(dso.Id, dso);
                }

                param.RightAscension = new DatabaseInteraction.DeepSkyObjectSearchFromThru<double?> {
                    From = 0,
                    Thru = viewportFoV.TopLeft.RADegrees
                };
            }

            foreach (var dso in await dbInstance.GetDeepSkyObjects(
                string.Empty, param, ct)) {
                dsoList.Add(dso.Id, dso);
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
                var frameLine = new FrameLine() { Closed = true, StrokeThickness = 1, Collection = new System.Windows.Media.PointCollection() };
                foreach (var coordinates in boundary.Value.Boundaries) {
                    var point = coordinates.GnomonicTanProjection(viewportFoV);
                    frameLine.Collection.Add(point);
                }
                if (frameLine.Collection.Any((x) => x.X > 0 && x.Y > 0 && x.X < viewportFoV.Width && x.Y < viewportFoV.Height)) {
                    ConstellationBoundariesInViewPort.Add(frameLine);
                }
            }
        }

        public async Task UpdateSkyMap(CancellationToken ct) {
            var allGatheredDSO = await GetDeepSkyObjectsForViewport(ct);

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
                FramingConstellation viewPortConstellation = null;
                foreach (var f in ConstellationsInViewport) {
                    if (f.Id != constellation.Id) {
                        continue;
                    }

                    viewPortConstellation = f;
                    break;
                }

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
                        viewPortConstellation.RecalculateStarPositions(viewportFoV);
                    }
                } else if (viewPortConstellation != null) {
                    ConstellationsInViewport.Remove(viewPortConstellation);
                }
            }

            var dsosToAdd = allGatheredDSO.Where(x => !existingDSOs.Any(y => y == x.Value.Id));
            foreach (var dso in dsosToAdd) {
                DSOInViewport.Add(new FramingDSO(dso.Value, viewportFoV));
            }
        }
    }
}