using NINA.Utility;
using NINA.Utility.Astrometry;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;

namespace NINA.ViewModel.FramingAssistant {

    public class FrameLine {
        public List<PointF> Collection { get; set; }

        public bool Closed { get; set; }

        public float StrokeThickness { get; set; }

        public Angle Angle { get; set; }
    }
}