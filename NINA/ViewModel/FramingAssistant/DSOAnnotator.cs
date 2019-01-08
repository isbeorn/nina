using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.ViewModel.FramingAssistant {

    internal class DSOAnnotator : BaseINPC {
        private readonly DatabaseInteraction dbInstance;
        private AsyncObservableCollection<FramingDSO> dsoInImage;
        private ViewportFoV viewportFoV;
        private FrameLineMatrix frameLineMatrix;

        public DSOAnnotator(string databaseLocation) {
            dbInstance = new DatabaseInteraction(databaseLocation);
            DSOInImage = new AsyncObservableCollection<FramingDSO>();
            FrameLineMatrix = new FrameLineMatrix();
        }

        public async void Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CancellationToken ct) {
            viewportFoV = new ViewportFoV(centerCoordinates, vFoVDegrees, imageWidth, imageHeight, imageRotation);

            ClearFrameLineMatrix();

            await UpdateDSOInImage(new Vector(0, 0), ct);

            FrameLineMatrix.CalculatePoints(viewportFoV);
        }

        public FrameLineMatrix FrameLineMatrix {
            get => frameLineMatrix;
            set {
                frameLineMatrix = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<FramingDSO> DSOInImage {
            get => dsoInImage;
            set {
                dsoInImage = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Query for skyobjects for a reference coordinate that overlap the current field of view
        /// </summary>
        /// <param name="referenceCoordinate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, DeepSkyObject>> GetDeepSkyObjectsForFoV(CancellationToken ct) {
            Dictionary<string, DeepSkyObject> dsoList = new Dictionary<string, DeepSkyObject>();

            using (MyStopWatch.Measure(nameof(GetDeepSkyObjectsForFoV))) {
                DatabaseInteraction.DeepSkyObjectSearchParams param =
                    new DatabaseInteraction.DeepSkyObjectSearchParams();

                // calculate size, at 10deg fov we want all items, at 45deg fov only the items that are larger than 100
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
            }

            return dsoList;
        }

        public Coordinates ShiftViewport(Vector delta) {
            using (MyStopWatch.Measure(nameof(ShiftViewport))) {
                viewportFoV.ShiftViewport(delta);
            }

            return viewportFoV.CenterCoordinates;
        }

        public void ClearFrameLineMatrix() {
            FrameLineMatrix.RAPoints.Clear();
            FrameLineMatrix.DecPoints.Clear();
        }

        public void CalculateFrameLineMatrix() {
            using (MyStopWatch.Measure(nameof(CalculateFrameLineMatrix))) {
                FrameLineMatrix.CalculatePoints(viewportFoV);
                GC.Collect();
            }
        }

        public async Task UpdateDSOInImage(Vector delta, CancellationToken ct) {
            using (MyStopWatch.Measure(nameof(UpdateDSOInImage))) {
                ShiftViewport(delta);

                var allGatheredDSO = await GetDeepSkyObjectsForFoV(ct);

                var existingDSOs = new List<string>();
                for (int i = DSOInImage.Count - 1; i >= 0; i--) {
                    var dso = DSOInImage[i];
                    if (allGatheredDSO.ContainsKey(dso.Id)) {
                        dso.RecalculateTopLeft(viewportFoV);
                        existingDSOs.Add(dso.Id);
                    } else {
                        DSOInImage.RemoveAt(i);
                    }
                }

                var dsosToAdd = allGatheredDSO.Where(x => !existingDSOs.Any(y => y == x.Value.Id));
                foreach (var dso in dsosToAdd) {
                    DSOInImage.Add(new FramingDSO(dso.Value, viewportFoV));
                }
            }
        }
    }
}