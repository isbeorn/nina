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

using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using System;

namespace NINA.ViewModel.FlatWizard {

    internal class FlatWizardFilterSettingsWrapper : BaseINPC {
        private FilterInfo filterInfo;

        private bool isChecked = false;

        private FlatWizardFilterSettings settings;

        private CameraInfo cameraInfo;

        public FlatWizardFilterSettingsWrapper(FilterInfo filterInfo, FlatWizardFilterSettings settings) {
            this.filterInfo = filterInfo;
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
                return (settings.HistogramMeanTarget * Math.Pow(2, CameraInfo.BitDepth)).ToString("0");
            }
        }

        public string HistogramToleranceADU {
            get {
                double histogrammean = settings.HistogramMeanTarget * Math.Pow(2, CameraInfo.BitDepth);
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

        internal CameraInfo CameraInfo {
            get => cameraInfo;
            set => {
                cameraInfo = value;
                RaisePropertyChanged(nameof(HistogramMeanTargetADU));
                RaisePropertyChanged(nameof(HistogramToleranceADU));
            }
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            RaisePropertyChanged(nameof(HistogramMeanTargetADU));
            RaisePropertyChanged(nameof(HistogramToleranceADU));
            if (Filter?.FlatWizardFilterSettings != null) {
                Filter.FlatWizardFilterSettings = Settings;
            }
        }
    }
}