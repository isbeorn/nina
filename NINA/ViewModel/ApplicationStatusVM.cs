using NINA.Model;
using NINA.Utility.Mediator;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class ApplicationStatusVM : DockableVM {

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

        private static Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterRequest(
                new StatusUpdateMessageHandle((StatusUpdateMessage msg) => {
                    _dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                        var status = msg.Status;
                        var item = ApplicationStatus.Where((x) => x.Source == status.Source).FirstOrDefault();
                        if (item != null) {
                            if (!string.IsNullOrEmpty(status.Status)) {
                                item.Status = status.Status;
                                item.Progress = status.Progress;
                                item.ProgressType = status.ProgressType;
                                item.MaxProgress = status.MaxProgress;
                            } else {
                                ApplicationStatus.Remove(item);
                            }
                        } else {
                            if (!string.IsNullOrEmpty(status.Status)) {
                                ApplicationStatus.Add(new ApplicationStatus() {
                                    Source = status.Source,
                                    Status = status.Status,
                                    Progress = status.Progress,
                                    MaxProgress = status.MaxProgress,
                                    ProgressType = status.ProgressType
                                });
                            }
                        }

                        RaisePropertyChanged(nameof(ApplicationStatus));
                    }));
                    return true;
                })
            );
        }
    }
}