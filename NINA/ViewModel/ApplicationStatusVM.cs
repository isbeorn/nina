#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class ApplicationStatusVM : DockableVM, IApplicationStatusVM {

        public ApplicationStatusVM(IProfileService profileService, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblApplicationStatus";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ApplicationStatusSVG"];

            this.applicationStatusMediator = applicationStatusMediator;
            this.applicationStatusMediator.RegisterHandler(this);
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
        private IApplicationStatusMediator applicationStatusMediator;

        public void StatusUpdate(ApplicationStatus status) {
            _dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                var item = ApplicationStatus.Where((x) => x.Source == status.Source).FirstOrDefault();
                if (item != null) {
                    if (!string.IsNullOrEmpty(status.Status)) {
                        item.Status = status.Status;
                        item.Progress = status.Progress;
                        item.MaxProgress = status.MaxProgress;
                        item.ProgressType = status.ProgressType;
                        item.Status2 = status.Status2;
                        item.Progress2 = status.Progress2;
                        item.MaxProgress2 = status.MaxProgress2;
                        item.ProgressType2 = status.ProgressType2;
                        item.Status3 = status.Status3;
                        item.Progress3 = status.Progress3;
                        item.MaxProgress3 = status.MaxProgress3;
                        item.ProgressType3 = status.ProgressType3;
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
                            ProgressType = status.ProgressType,
                            Status2 = status.Status2,
                            Progress2 = status.Progress2,
                            MaxProgress2 = status.MaxProgress2,
                            ProgressType2 = status.ProgressType2,
                            Status3 = status.Status3,
                            Progress3 = status.Progress3,
                            MaxProgress3 = status.MaxProgress3,
                            ProgressType3 = status.ProgressType3
                        });
                    }
                }

                RaisePropertyChanged(nameof(ApplicationStatus));
            }));
        }
    }
}