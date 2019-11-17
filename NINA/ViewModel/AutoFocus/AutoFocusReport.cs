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
        public string Method { get; set; }

        [JsonProperty]
        public string Fitting { get; set; }

        [JsonProperty]
        public FocusPoint InitialFocusPoint { get; set; }

        [JsonProperty]
        public FocusPoint CalculatedFocusPoint { get; set; }

        [JsonProperty]
        public FocusPoint PreviousFocusPoint { get; set; }

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