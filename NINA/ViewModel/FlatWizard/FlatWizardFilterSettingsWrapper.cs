using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using System;

namespace NINA.ViewModel.FlatWizard {

    internal class FlatWizardFilterSettingsWrapper : BaseINPC, ICameraConsumer {
        private FilterInfo filterInfo;

        private bool isChecked = false;

        private FlatWizardFilterSettings settings;

        private CameraInfo cameraInfo;

        public FlatWizardFilterSettingsWrapper(FilterInfo filterInfo, FlatWizardFilterSettings settings, ICameraMediator cameraMediator) {
            this.filterInfo = filterInfo;
            this.settings = settings;
            cameraMediator.RegisterConsumer(this);
            settings.PropertyChanged += Settings_PropertyChanged;
        }

        public FilterInfo Filter {
            get {
                return filterInfo;
            }
            set {
                filterInfo = value;
                RaisePropertyChanged();
            }
        }

        public string HistogramMeanTargetADU {
            get {
                return (settings.HistogramMeanTarget * Math.Pow(2, cameraInfo.BitDepth)).ToString("0");
            }
        }

        public string HistogramToleranceADU {
            get {
                double histogrammean = settings.HistogramMeanTarget * Math.Pow(2, cameraInfo.BitDepth);
                return (histogrammean - histogrammean * settings.HistogramTolerance).ToString("0") + " - " + (histogrammean + histogrammean * settings.HistogramTolerance).ToString("0");
            }
        }

        public bool IsChecked {
            get {
                return isChecked;
            }

            set {
                isChecked = value;
                RaisePropertyChanged();
            }
        }

        public FlatWizardFilterSettings Settings {
            get {
                return settings;
            }
            set {
                settings = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            cameraInfo = deviceInfo;
            RaisePropertyChanged(nameof(HistogramMeanTargetADU));
            RaisePropertyChanged(nameof(HistogramToleranceADU));
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            RaisePropertyChanged("HistogramMeanTargetADU");
            RaisePropertyChanged("HistogramToleranceADU");
            if (Filter?.FlatWizardFilterSettings != null) {
                Filter.FlatWizardFilterSettings = Settings;
            }
        }
    }
}