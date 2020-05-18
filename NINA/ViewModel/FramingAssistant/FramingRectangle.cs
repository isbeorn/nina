using NINA.Utility;
using NINA.Utility.Astrometry;

namespace NINA.ViewModel.FramingAssistant {

    internal class FramingRectangle : ObservableRectangle {

        public FramingRectangle(double rotationOffset) : base(rotationOffset) {
        }

        public FramingRectangle(double x, double y, double width, double height) : base(x, y, width, height) {
        }

        private int id;

        public int Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates coordinates;

        public Coordinates Coordinates {
            get {
                return coordinates;
            }
            set {
                coordinates = value;
                RaisePropertyChanged();
            }
        }
    }
}