#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Utility.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.AutoFocus {

    public class AutoFocusReport {

        [JsonProperty]
        public DateTime Timestamp { get; set; }

        [JsonProperty]
        public double Temperature { get; set; }

        [JsonProperty]
        public string Method { get; set; }

        [JsonProperty]
        public string Fitting { get; set; }

        [JsonProperty]
        public FocusPoint InitialFocusPoint { get; set; } = new FocusPoint();

        [JsonProperty]
        public FocusPoint CalculatedFocusPoint { get; set; } = new FocusPoint();

        [JsonProperty]
        public FocusPoint PreviousFocusPoint { get; set; } = new FocusPoint();

        [JsonProperty]
        public IEnumerable<FocusPoint> MeasurePoints { get; set; }

        [JsonProperty]
        public Intersections Intersections { get; set; }
    }

    public class Intersections {
        public FocusPoint TrendLineIntersection { get; set; }
        public FocusPoint HyperbolicMinimum { get; set; }
        public FocusPoint QuadraticMinimum { get; set; }
        public FocusPoint GaussianMaximum { get; set; }
    }

    public class FocusPoint {

        [JsonProperty]
        public double Position { get; set; }

        [JsonProperty]
        public double Value { get; set; }

        [JsonProperty]
        public double Error { get; set; }
    }
}