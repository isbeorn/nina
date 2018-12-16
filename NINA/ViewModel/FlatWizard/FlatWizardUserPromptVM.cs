#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
            private set {
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