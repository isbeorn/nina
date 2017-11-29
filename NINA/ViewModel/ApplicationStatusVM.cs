using NINA.Utility.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {
    class ApplicationStatusVM : DockableVM {
        public ApplicationStatusVM() {
            Title = "LblApplicationStatus";
            ContentId = nameof(ApplicationStatusVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ApplicationStatusSVG"];

            RegisterMediatorMessages();
        }

        private string _status;
        public string Status {
            get {
                return _status;
            }
            private set {
                _status = value;
                RaisePropertyChanged();
            }
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterRequest(
                new StatusUpdateMessageHandle((StatusUpdateMessage msg) => {
                    Status = msg.Status;
                    return true;
                })
            );
        }
    }
}
