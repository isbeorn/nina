using NINA.Utility;
using System.Windows.Media;

namespace NINA.ViewModel.FramingAssistant {

    public class FrameLine : BaseINPC {
        private PointCollection collection;
        private bool closed;
        private double _strokeThickness;

        public PointCollection Collection {
            get => collection;
            set {
                collection = value;
                RaisePropertyChanged();
            }
        }

        public bool Closed {
            get => closed;
            set {
                closed = value;
                RaisePropertyChanged();
            }
        }

        public double StrokeThickness {
            get => _strokeThickness;
            set {
                _strokeThickness = value;
                RaisePropertyChanged();
            }
        }
    }
}