#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.PlateSolving;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {

    public class PlateSolvingStatusVM : BaseINPC {

        public PlateSolvingStatusVM() {
            Progress = new Progress<PlateSolveProgress>(x => {
                if (x.Thumbnail != null) {
                    Thumbnail = x.Thumbnail;
                }
                if (x.PlateSolveResult != null) {
                    PlateSolveResult = x.PlateSolveResult;
                }
            });
        }

        public string Title { get => Locale.Loc.Instance["LblPlateSolving"]; }

        private PlateSolveResult plateSolveResult;

        public IProgress<PlateSolveProgress> Progress { get; }

        public PlateSolveResult PlateSolveResult {
            get {
                return plateSolveResult;
            }

            set {
                plateSolveResult = value;
                if (value != null) {
                    var existingItem = PlateSolveHistory.FirstOrDefault(x => x.SolveTime == value.SolveTime);
                    if (existingItem != null) {
                        //In case an existing item is set again
                        var index = PlateSolveHistory.IndexOf(existingItem);
                        PlateSolveHistory[index] = existingItem;
                    } else {
                        PlateSolveHistory.Add(value);
                    }
                }
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<PlateSolveResult> plateSolveHistory = new AsyncObservableCollection<PlateSolveResult>();

        public AsyncObservableCollection<PlateSolveResult> PlateSolveHistory {
            get => plateSolveHistory;
            private set {
                plateSolveHistory = value;
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