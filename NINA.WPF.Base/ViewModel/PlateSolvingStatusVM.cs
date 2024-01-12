#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.PlateSolving;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Core.Locale;
using NINA.Core.Model;

namespace NINA.WPF.Base.ViewModel {

    public class PlateSolvingStatusVM : BaseINPC {

        public PlateSolvingStatusVM() {
            PlateSolveHistory = new AsyncObservableCollection<PlateSolveResult>();
            Progress = new Progress<PlateSolveProgress>(x => {
                if (x.Thumbnail != null) {
                    Thumbnail = x.Thumbnail;
                }
                if (x.PlateSolveResult != null) {
                    PlateSolveResult = x.PlateSolveResult;
                }
            });
        }
        public AsyncObservableCollection<PlateSolveResult> PlateSolveHistory { get; }

        public string Title => Loc.Instance["LblPlateSolving"];


        public IProgress<PlateSolveProgress> Progress { get; }

        public IProgress<ApplicationStatus> CreateLinkedProgress(IProgress<ApplicationStatus> original) {
            return new Progress<ApplicationStatus>(x => {
                Status = x;
                original?.Report(x);
            });
        }

        private ApplicationStatus status;
        public ApplicationStatus Status {
            get => status;
            set {
                status = value;
                RaisePropertyChanged();
            }
        }

        private object lockObj = new object();
        private PlateSolveResult plateSolveResult;
        public PlateSolveResult PlateSolveResult {
            get => plateSolveResult;

            set {
                plateSolveResult = value;
                if (value != null) {     
                   lock (lockObj) {
                        var existingItem = PlateSolveHistory.FirstOrDefault(x => x.SolveTime == value.SolveTime);
                        if (existingItem != null) {
                            //In case an existing item is set again
                            var index = PlateSolveHistory.IndexOf(existingItem);
                            PlateSolveHistory[index] = existingItem;
                        } else {
                            PlateSolveHistory.Add(value);
                        }
                    }
                }
                RaisePropertyChanged();
            }
        }


        private BitmapSource thumbnail;

        public BitmapSource Thumbnail {
            get => thumbnail;
            set {
                thumbnail = value;
                RaisePropertyChanged();
            }
        }
    }
}