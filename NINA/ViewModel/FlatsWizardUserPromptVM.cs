using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    internal class FlatsWizardUserPromptVM : BaseINPC {
        private readonly string text;
        private readonly double currentMean;
        private readonly double cameraBitDepth;
        private double tolerance;
        private bool reset;
        private double histogramMean;
        private double minimumTime;
        private double maximumTime;
        private bool continueWizard;
        private readonly double expectedExposureTime;

        public RelayCommand ContinueCommand { get; }
        public RelayCommand CancelCommand { get; }

        public FlatsWizardUserPromptVM(string text, double currentMean, double cameraBitDepth, double tolerance, double histogramMean, double minimumTime, double maximumTime, double expectedExposureTime) {
            this.text = text;
            this.currentMean = currentMean;
            this.cameraBitDepth = cameraBitDepth;
            this.tolerance = tolerance;
            this.histogramMean = histogramMean;
            this.expectedExposureTime = expectedExposureTime;
            this.minimumTime = minimumTime;
            this.maximumTime = maximumTime;
            ContinueCommand = new RelayCommand(ContinueFlatsWizard);
            CancelCommand = new RelayCommand(CancelFlatsWizard);
        }

        private void CancelFlatsWizard(object obj) {
            continueWizard = false;
        }

        private void ContinueFlatsWizard(object obj) {
            continueWizard = true;
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

        public double Tolerance {
            get {
                return tolerance;
            }
            set {
                tolerance = value;
                RaisePropertyChanged();
            }
        }

        public double HistogramMean {
            get {
                return histogramMean;
            }
            set {
                histogramMean = value;
                RaisePropertyChanged();
            }
        }

        public double MinimumTime {
            get {
                return minimumTime;
            }
            set {
                minimumTime = value;
                RaisePropertyChanged();
            }
        }

        public double MaximumTime {
            get {
                return maximumTime;
            }
            set {
                maximumTime = value;
                RaisePropertyChanged();
            }
        }

        public double ExpectedExposureTime1 { get; }
    }
}