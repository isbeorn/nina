using NINA.Utility;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {

    public class FlatWizardFilterSettingsWrapper : BaseINPC {
        private readonly IProfileService profileService;

        private FilterInfo filterInfo;

        private bool isChecked = false;

        private FlatWizardFilterSettings settings;

        public FlatWizardFilterSettingsWrapper(FilterInfo filterInfo, FlatWizardFilterSettings settings, IProfileService profileService) {
            this.filterInfo = filterInfo;
            this.profileService = profileService;
            this.settings = settings;
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
                return (settings.HistogramMeanTarget * Math.Pow(2, profileService.ActiveProfile.CameraSettings.BitDepth)).ToString("0");
            }
        }

        public string HistogramToleranceADU {
            get {
                double histogrammean = settings.HistogramMeanTarget * Math.Pow(2, profileService.ActiveProfile.CameraSettings.BitDepth);
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

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            RaisePropertyChanged("HistogramMeanTargetADU");
            RaisePropertyChanged("HistogramToleranceADU");
            if (Filter?.FlatWizardFilterSettings != null) {
                Filter.FlatWizardFilterSettings = Settings;
            }
        }
    }
}