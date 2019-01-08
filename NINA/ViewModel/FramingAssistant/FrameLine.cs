using NINA.Utility;
using System.Windows.Media;

namespace NINA.ViewModel.FramingAssistant {

    public class FrameLine : BaseINPC {
        private PointCollection collection;
        private bool closed;
        private double stroke;

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

        public double Stroke {
            get => stroke;
            set {
                stroke = value;
                RaisePropertyChanged();
            }
        }
    }
}