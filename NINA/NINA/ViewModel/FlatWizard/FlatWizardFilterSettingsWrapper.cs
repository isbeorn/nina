#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Core.Utility;
using NINA.Core.Model.Equipment;
using NINA.Image.ImageAnalysis;

namespace NINA.ViewModel.FlatWizard {

    public class FlatWizardFilterSettingsWrapper : BaseINPC {
        private FilterInfo filterInfo;

        private bool isChecked = false;

        private FlatWizardFilterSettings settings;
        private int bitDepth;
        private CameraInfo cameraInfo;
        private FlatDeviceInfo flatDeviceInfo;

        public FlatWizardFilterSettingsWrapper(FilterInfo filterInfo, FlatWizardFilterSettings settings, int bitDepth, CameraInfo cameraInfo, FlatDeviceInfo flatDeviceInfo) {
            this.filterInfo = filterInfo;
            this.settings = settings;
            this.bitDepth = bitDepth;
            this.cameraInfo = cameraInfo;
            this.flatDeviceInfo = flatDeviceInfo;
            settings.PropertyChanged += Settings_PropertyChanged;
        }

        public int BitDepth {
            get => bitDepth;
            set {
                bitDepth = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HistogramMeanTargetADU));
                RaisePropertyChanged(nameof(HistogramToleranceADU));
            }
        }

        public CameraInfo CameraInfo {
            get => cameraInfo;
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public FlatDeviceInfo FlatDeviceInfo {
            get => flatDeviceInfo;
            set {
                flatDeviceInfo = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo Filter {
            get => filterInfo;
            set {
                filterInfo = value;
                RaisePropertyChanged();
            }
        }

        public string HistogramMeanTargetADU => HistogramMath.HistogramMeanAndCameraBitDepthToAdu(settings.HistogramMeanTarget, BitDepth).ToString("0");

        public string HistogramToleranceADU =>
            HistogramMath.GetLowerToleranceBoundInAdu(settings.HistogramMeanTarget, BitDepth, settings.HistogramTolerance).ToString("0")
            + " - " + HistogramMath.GetUpperToleranceBoundInAdu(settings.HistogramMeanTarget, BitDepth, settings.HistogramTolerance).ToString("0");

        public bool IsChecked {
            get => isChecked;

            set {
                isChecked = value;
                RaisePropertyChanged();
            }
        }

        public FlatWizardFilterSettings Settings {
            get => settings;
            set {
                settings = value;
                RaisePropertyChanged();
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