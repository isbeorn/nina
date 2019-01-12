using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NINA.ViewModel.FramingAssistant {

    internal class FramingConstellation : BaseINPC {
        private Constellation constellation;
        private Point centerPoint;

        public FramingConstellation(Constellation constellation, ViewportFoV viewport) {
            this.constellation = constellation;

            Id = constellation.Id;
            Name = constellation.Name;

            var constellationStartDec = constellation.Stars.Select(m => m.Coords.Dec).Min();
            var constellationStopDec = constellation.Stars.Select(m => m.Coords.Dec).Max();

            if (constellation.GoesOverRaZero) {
                double stopRA = 0;
                double startRA = 0;
                IEnumerable<IGrouping<bool, Star>> groups = constellation.Stars.GroupBy(s => s.Coords.RADegrees > 180);
                foreach (var group in groups) {
                    if (group.Key) {
                        stopRA = group.Select(m => m.Coords.RADegrees).Min();
                    } else {
                        startRA = group.Select(m => m.Coords.RADegrees).Max();
                    }
                }

                var distance = startRA + 360 - stopRA;

                var centerRa = stopRA + distance / 2;
                if (centerRa > 360) {
                    centerRa -= 360;
                }

                constellationCenter = new Coordinates(centerRa,
                    constellationStopDec + (constellationStartDec - constellationStopDec) / 2, Epoch.J2000, Coordinates.RAType.Degrees);
            } else {
                var constellationStartRA = constellation.Stars.Select(m => m.Coords.RADegrees).Min();
                var constellationStopRA = constellation.Stars.Select(m => m.Coords.RADegrees).Max();

                constellationCenter = new Coordinates(
                    constellationStopRA + (constellationStartRA - constellationStopRA) / 2,
                    constellationStopDec + (constellationStartDec - constellationStopDec) / 2, Epoch.J2000,
                    Coordinates.RAType.Degrees);
            }

            Points = new AsyncObservableCollection<Tuple<Star, Star>>();
            Stars = new AsyncObservableCollection<Star>();

            foreach (var star in constellation.Stars) {
                star.Radius = (-3.375 * star.Mag + 23.25) / (viewport.VFoVDeg / 8);
            }

            RecalculateConstellationPoints(viewport);
        }

        private List<FrameLine> starLines;

        private AsyncObservableCollection<Star> stars;
        private AsyncObservableCollection<Tuple<Star, Star>> points;
        private readonly Coordinates constellationCenter;

        public void RecalculateConstellationPoints(ViewportFoV reference) {
            // calculate all star positions for the constellation once and add them to the star collection for drawing if they're visible
            foreach (var star in constellation.Stars) {
                star.Position = star.Coords.GnomonicTanProjection(reference);
                var isInBounds = !reference.IsOutOfViewportBounds(star.Position);
                var index = Stars.IndexOf(star);
                if (isInBounds && index == -1) {
                    Stars.Add(star);
                } else if (!isInBounds && index > 0) {
                    Stars.Remove(star);
                }
            }

            // now we check what lines are visible in the fov and only add those connections as well
            foreach (var starConnection in constellation.StarConnections) {
                var isInBounds = !(reference.IsOutOfViewportBounds(starConnection.Item1.Position) &&
                                    reference.IsOutOfViewportBounds(starConnection.Item2.Position));
                var index = Points.IndexOf(starConnection);
                if (isInBounds && index == -1) {
                    Points.Add(starConnection);
                } else if (!isInBounds && index > 0) {
                    Points.Remove(starConnection);
                }
            }

            CenterPoint = constellationCenter.GnomonicTanProjection(reference);
        }

        public Point CenterPoint {
            get => centerPoint;
            set {
                centerPoint = value;
                RaisePropertyChanged();
            }
        }

        public string Id { get; }
        public string Name { get; }

        public AsyncObservableCollection<Star> Stars {
            get => stars;
            set {
                stars = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<Tuple<Star, Star>> Points {
            get => points;
            set {
                points = value;
                RaisePropertyChanged();
            }
        }
    }
}