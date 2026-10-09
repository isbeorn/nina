using NINA.Astrometry.Body;
using System;
using System.Collections.Generic;

namespace NINA.Astrometry.RiseAndSet {
    // Scoped to one complete night: exact samples and extrema can be shared by the four
    // solar thresholds without retaining stale observer or Earth-rotation data between calls.
    internal sealed class SolarEventContext(double latitude, double longitude, double elevation) {
        private readonly Dictionary<DateTime, Sun> samples = new();
        internal Dictionary<(DateTime Start, DateTime End, bool Minimum), DateTime> Extrema { get; } = new();

        internal Sun GetBody(DateTime time) {
            if (!samples.TryGetValue(time, out var sun)) {
                sun = new Sun(time, latitude, longitude, elevation);
                sun.Calculate();
                samples.Add(time, sun);
            }
            return sun;
        }
    }
}
