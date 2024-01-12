#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Profile.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using NINA.Core.Model;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using NINA.Core.Locale;

namespace NINA.ViewModel {

    internal class ApplicationStatusVM : DockableVM, IApplicationStatusVM {

        public ApplicationStatusVM(IProfileService profileService, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = Loc.Instance["LblApplicationStatus"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ApplicationStatusSVG"];

            this.applicationStatusMediator = applicationStatusMediator;
            this.applicationStatusMediator.RegisterHandler(this);
        }

        private string _status;

        public string Status {
            get => _status;
            private set {
                _status = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ApplicationStatus> _applicationStatus = new ObservableCollection<ApplicationStatus>();

        public ObservableCollection<ApplicationStatus> ApplicationStatus {
            get => _applicationStatus;
            set {
                _applicationStatus = value;
                RaisePropertyChanged();
            }
        }

        private static Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        private IApplicationStatusMediator applicationStatusMediator;

        public void StatusUpdate(ApplicationStatus status) {
            _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                try {
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
                } catch { }
            }));
        }
    }
}