#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;

namespace NINA.ViewModel.FlatWizard {

    internal class FlatWizardUserPromptVM : BaseINPC {
        private readonly string text;
        private readonly double currentMean;
        private readonly double cameraBitDepth;
        private bool continueWizard;
        private FlatWizardFilterSettingsWrapper settings;

        private bool reset;
        private readonly double expectedExposureTime;

        public RelayCommand ResetAndContinueCommand { get; }
        public RelayCommand ContinueCommand { get; }
        public RelayCommand CancelCommand { get; }

        public FlatWizardUserPromptVM(string text, double currentMean, double cameraBitDepth, FlatWizardFilterSettingsWrapper settings, double expectedExposureTime) {
            this.text = text;
            this.currentMean = currentMean;
            this.cameraBitDepth = cameraBitDepth;
            this.expectedExposureTime = expectedExposureTime;
            this.settings = settings;
            ResetAndContinueCommand = new RelayCommand(ResetAndContinueContinueFlatWizard);
            ContinueCommand = new RelayCommand(ContinueFlatWizard);
            CancelCommand = new RelayCommand(CancelFlatWizard);
        }

        private void ResetAndContinueContinueFlatWizard(object obj) {
            Continue = true;
            Reset = true;
        }

        private void CancelFlatWizard(object obj) {
            Continue = false;
        }

        private void ContinueFlatWizard(object obj) {
            Continue = true;
        }

        public FlatWizardFilterSettingsWrapper Settings {
            get {
                return settings;
            }
            set {
                settings = value;
                RaisePropertyChanged();
            }
        }

        public bool Continue {
            get {
                return continueWizard;
            }
            set {
                continueWizard = value;
            }
        }

        public string Text {
            get {
                return text;
            }
        }

        public double CurrentMean {
            get {
                return currentMean;
            }
        }

        public double CameraBitDepth {
            get {
                return cameraBitDepth;
            }
        }

        public double ExpectedExposureTime {
            get {
                return expectedExposureTime;
            }
        }

        public bool Reset {
            get {
                return reset;
            }
            set {
                reset = value;
                RaisePropertyChanged();
            }
        }
    }
}