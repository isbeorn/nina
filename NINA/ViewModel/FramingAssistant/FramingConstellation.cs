using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NINA.ViewModel.FramingAssistant {

    internal class FramingConstellation {
        private Constellation constellation;

        public FramingConstellation(Constellation constellation, ViewportFoV viewport) {
            this.constellation = constellation;

            Id = constellation.Id;
            Name = constellation.Name;

            var constellationStartDec = constellation.Stars.Select(m => m.Coords.Dec).Min();
            var constellationStopDec = constellation.Stars.Select(m => m.Coords.Dec).Max();

            if (constellation.GoesOverRaZero) {
                double stopRA = double.MaxValue;
                double startRA = 0;
                foreach (var star in constellation.Stars) {
                    if (star.Coords.RADegrees > 180) {
                        stopRA = Math.Min(stopRA, star.Coords.RADegrees);
                    } else {
                        startRA = Math.Max(startRA, star.Coords.RADegrees);
                    }
                }
                if (stopRA == double.MaxValue) {
                    stopRA = 0;
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

            Points = new HashSet<Tuple<Star, Star>>();
            Stars = new HashSet<Star>();

            foreach (var star in constellation.Stars) {
                star.Radius = (-3.375 * star.Mag + 23.25) / (viewport.VFoVDeg / 8);
            }

            RecalculateConstellationPoints(viewport);
        }

        private readonly Coordinates constellationCenter;

        public void RecalculateConstellationPoints(ViewportFoV reference) {
            // calculate all star positions for the constellation once and add them to the star collection for drawing if they're visible
            foreach (var star in constellation.Stars) {
                star.Position = star.Coords.GnomonicTanProjection(reference);
                var isInBounds = !reference.IsOutOfViewportBounds(star.Position);
                var contains = Stars.Contains(star);
                if (isInBounds && !contains) {
                    Stars.Add(star);
                } else if (!isInBounds && contains) {
                    Stars.Remove(star);
                }
            }

            // now we check what lines are visible in the fov and only add those connections as well
            foreach (var starConnection in constellation.StarConnections) {
                var isInBounds = !(reference.IsOutOfViewportBounds(starConnection.Item1.Position) &&
                                    reference.IsOutOfViewportBounds(starConnection.Item2.Position));
                var contains = Points.Contains(starConnection);
                if (isInBounds && !contains) {
                    Points.Add(starConnection);
                } else if (!isInBounds && contains) {
                    Points.Remove(starConnection);
                }
            }

            CenterPoint = constellationCenter.GnomonicTanProjection(reference);
        }

        public Point CenterPoint { get; private set; }

        public string Id { get; }
        public string Name { get; }

        public HashSet<Star> Stars { get; private set; }

        public HashSet<Tuple<Star, Star>> Points { get; private set; }
    }
}