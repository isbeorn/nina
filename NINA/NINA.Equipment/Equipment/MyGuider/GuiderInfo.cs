#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

namespace NINA.Equipment.Equipment.MyGuider {

    public class GuiderInfo : DeviceInfo {
        private bool _canClearCalibration;

        public bool CanClearCalibration {
            get => _canClearCalibration;
            set {
                if (_canClearCalibration != value) {
                    _canClearCalibration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _canSetShiftRate;

        public bool CanSetShiftRate {
            get => _canSetShiftRate;
            set {
                if (_canSetShiftRate != value) {
                    _canSetShiftRate = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _canGetLockPosition;

        public bool CanGetLockPosition {
            get => _canGetLockPosition;
            set {
                _canGetLockPosition = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> supportedActions;

        public IList<string> SupportedActions {
            get => supportedActions;
            set {
                if (supportedActions != value) {
                    supportedActions = value;
                    RaisePropertyChanged();
                }
            }
        }

        private RMSError rmsError;
        /// <summary>
        /// The RMS Error of all datapoints in the GuideStepHistory. The values depend on the value range selection for the guide charts
        /// </summary>
        public RMSError RMSError {
            get => rmsError;
            set {
                if (rmsError != value) {
                    rmsError = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double pixelScale;
        public double PixelScale {
            get => pixelScale;
            set {
                pixelScale = value;
                RaisePropertyChanged();
            }
        }
    }
}