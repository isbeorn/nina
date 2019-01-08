using NINA.Utility;
using System.Windows.Media;

namespace NINA.ViewModel.FramingAssistant {

    public class PointCollectionAndClosed : BaseINPC {
        private PointCollection collection;
        private bool closed;

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
    }
}