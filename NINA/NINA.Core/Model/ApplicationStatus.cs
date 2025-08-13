#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;

namespace NINA.Core.Model {

    public class ApplicationStatus : BaseINPC {
        private string _source;

        public string Source {
            get => _source;
            set {
                _source = value;
                RaisePropertyChanged();
            }
        }

        private string _status;

        public string Status {
            get => _status;
            set {
                _status = value;
                RaisePropertyChanged();
            }
        }

        private double _progress = -1;

        public double Progress {
            get => _progress;
            set {
                _progress = value;
                RaisePropertyChanged();
            }
        }

        private int _maxProgress = 1;

        public int MaxProgress {
            get => _maxProgress;
            set {
                _maxProgress = value;
                RaisePropertyChanged();
            }
        }

        private StatusProgressType _progressType = StatusProgressType.Percent;

        public StatusProgressType ProgressType {
            get => _progressType;
            set {
                _progressType = value;
                RaisePropertyChanged();
            }
        }

        private string _status2;

        public string Status2 {
            get => _status2;
            set {
                _status2 = value;
                RaisePropertyChanged();
            }
        }

        private double _progress2 = -1;

        public double Progress2 {
            get => _progress2;
            set {
                _progress2 = value;
                RaisePropertyChanged();
            }
        }

        private int _maxProgress2 = 1;

        public int MaxProgress2 {
            get => _maxProgress2;
            set {
                _maxProgress2 = value;
                RaisePropertyChanged();
            }
        }

        private StatusProgressType _progressType2 = StatusProgressType.Percent;

        public StatusProgressType ProgressType2 {
            get => _progressType2;
            set {
                _progressType2 = value;
                RaisePropertyChanged();
            }
        }

        private string _status3;

        public string Status3 {
            get => _status3;
            set {
                _status3 = value;
                RaisePropertyChanged();
            }
        }

        private double _progress3 = -1;

        public double Progress3 {
            get => _progress3;
            set {
                _progress3 = value;
                RaisePropertyChanged();
            }
        }

        private int _maxProgress3 = 1;

        public int MaxProgress3 {
            get => _maxProgress3;
            set {
                _maxProgress3 = value;
                RaisePropertyChanged();
            }
        }

        private StatusProgressType _progressType3 = StatusProgressType.Percent;

        public StatusProgressType ProgressType3 {
            get => _progressType3;
            set {
                _progressType3 = value;
                RaisePropertyChanged();
            }
        }

        public enum StatusProgressType {
            Percent,
            ValueOfMaxValue
        }
    }
}