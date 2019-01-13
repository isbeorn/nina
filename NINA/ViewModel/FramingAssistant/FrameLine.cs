using NINA.Utility;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;

namespace NINA.ViewModel.FramingAssistant {

    public class FrameLine {
        private PointCollection collection;
        private bool closed;
        private double _strokeThickness;

        public List<PointF> Collection { get; set; }

        public bool Closed { get; set; }

        public double StrokeThickness { get; set; }
    }
}