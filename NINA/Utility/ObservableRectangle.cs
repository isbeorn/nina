using System;

namespace NINA.Utility {

    public class ObservableRectangle : BaseINPC {

        public ObservableRectangle(double x, double y, double width, double height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public ObservableRectangle(double rotationOffset) {
            _rotationOffset = rotationOffset;
        }

        private double _x;

        public double X {
            get {
                return _x;
            }
            set {
                _x = value;
                RaisePropertyChanged();
            }
        }

        private double _y;

        public double Y {
            get {
                return _y;
            }
            set {
                _y = value;
                RaisePropertyChanged();
            }
        }

        private double _width;

        public double Width {
            get {
                return _width;
            }
            set {
                _width = value;
                RaisePropertyChanged();
            }
        }

        private double _height;

        public double Height {
            get {
                return _height;
            }
            set {
                _height = value;
                RaisePropertyChanged();
            }
        }

        private double _rotation;

        public double Rotation {
            get {
                return _rotation;
            }
            set {
                _rotation = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DisplayedRotation));
            }
        }

        private double _rotationOffset;

        public double DisplayedRotation {
            get {
                var rotation = Rotation - _rotationOffset;
                if (rotation < 0) {
                    rotation += 360;
                } else if (rotation >= 360) {
                    rotation -= 360;
                }
                return Math.Round(rotation, 2);
            }
        }
    }
}