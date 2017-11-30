using NINA.Utility;
using NINA.Utility.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private ObservableCollection<ApplicationStatus> _applicationStatus = new ObservableCollection<ApplicationStatus>();
        public ObservableCollection<ApplicationStatus> ApplicationStatus {
            get {
                return _applicationStatus;
            }
            set {
                _applicationStatus = value;
                RaisePropertyChanged();
            }
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterRequest(
                new StatusUpdateMessageHandle((StatusUpdateMessage msg) => {

                    var item = ApplicationStatus.Where((x) => x.Source == msg.Source).FirstOrDefault();
                    if(item != null) {
                        item.Status = msg.Status;
                    } else {
                        ApplicationStatus.Add(new ApplicationStatus() { Source = msg.Source, Status = msg.Status });
                    }
                    

                    RaisePropertyChanged(nameof(ApplicationStatus));
                    return true;
                })
            );
        }
    }

    public class ApplicationStatus : BaseINPC {
        private string _source;
        public string Source {
            get {
                return _source;
            }
            set {
                _source = value;
                RaisePropertyChanged();
            }
        }

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();
            }
        }
    }
}
