#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Immutable;

namespace NINA.Image.Interfaces {

    public interface IImageStatistics {
        int BitDepth { get; }
        double StDev { get; }
        double Mean { get; }
        double Median { get; }
        double MedianAbsoluteDeviation { get; }
        int Max { get; }
        long MaxOccurrences { get; }
        int Min { get; }
        long MinOccurrences { get; }
        ImmutableList<OxyPlot.DataPoint> Histogram { get; }
    }
}