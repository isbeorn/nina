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

                centerCoordinates = new Coordinates(centerRa,
                    constellationStopDec + (constellationStartDec - constellationStopDec) / 2, Epoch.J2000, Coordinates.RAType.Degrees);
            } else {
                var constellationStartRA = constellation.Stars.Select(m => m.Coords.RADegrees).Min();
                var constellationStopRA = constellation.Stars.Select(m => m.Coords.RADegrees).Max();

                centerCoordinates = new Coordinates(
                    constellationStopRA + (constellationStartRA - constellationStopRA) / 2,
                    constellationStopDec + (constellationStartDec - constellationStopDec) / 2, Epoch.J2000,
                    Coordinates.RAType.Degrees);
            }

            Points = new AsyncObservableCollection<Tuple<Point, Point>>();

            RecalculateConstellationPoints(viewport);
        }

        private List<FrameLine> starLines;

        private AsyncObservableCollection<Tuple<Point, Point>> points;
        private Coordinates centerCoordinates;

        public void RecalculateConstellationPoints(ViewportFoV reference) {
            Points.Clear();
            foreach (var starConnection in constellation.StarConnections) {
                var point1 = starConnection.Item1.Coords.GnomonicTanProjection(reference);
                var point2 = starConnection.Item2.Coords.GnomonicTanProjection(reference);
                if (!(reference.IsOutOfBounds(point1) && reference.IsOutOfBounds(point2))) {
                    Points.Add(new Tuple<Point, Point>(point1, point2));
                }
            }

            CenterPoint = centerCoordinates.GnomonicTanProjection(reference);
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

        public AsyncObservableCollection<Tuple<Point, Point>> Points {
            get => points;
            set {
                points = value;
                RaisePropertyChanged();
            }
        }
    }
}