using NINA.Model.MyFilterWheel;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {

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
            continueWizard = true;
            Reset = true;
        }

        private void CancelFlatWizard(object obj) {
            continueWizard = false;
        }

        private void ContinueFlatWizard(object obj) {
            continueWizard = true;
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

        public double ExpectedExposureTime1 { get; }
    }
}