#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using System.Collections.ObjectModel;
using System.Linq;
using NINA.Model.MyCamera;
using NINA.Utility;
using System.Collections.Generic;

namespace NINA.ViewModel.Equipment.FlatDevice {

    public class WizardGrid : BaseINPC {
        public AsyncObservableCollection<WizardValueBlock> Blocks { get; set; } = new AsyncObservableCollection<WizardValueBlock>();

        public void AddBlock(WizardValueBlock block) {
            Blocks.Add(block);
            RaisePropertyChanged(nameof(Blocks));
        }

        public void RemoveBlocks(ICollection<WizardValueBlock> blocks) {
            if (blocks == null) return;
            var changed = false;
            foreach (var block in blocks) {
                Blocks.Remove(block);
                changed = true;
            }
            if (changed) RaisePropertyChanged(nameof(Blocks));
        }
    }

    public class WizardValueBlock : BaseINPC {
        public BinningMode Binning { get; set; }

        public AsyncObservableCollection<WizardGridColumn> Columns { get; set; } = new AsyncObservableCollection<WizardGridColumn>();

        public void AddColumn(WizardGridColumn column) {
            Columns.Add(column);
            RaisePropertyChanged(nameof(Columns));
        }

        public void RemoveColumns(ICollection<WizardGridColumn> columns) {
            if (columns == null) return;
            var changed = false;
            foreach (var column in columns) {
                Columns.Remove(column);
                changed = true;
            }
            if (changed) RaisePropertyChanged(nameof(Columns));
        }
    }

    public class WizardGridColumn : BaseINPC {
        private int _columnNumber;
        private string _header;
        private int _gain;

        public int ColumnNumber {
            get => _columnNumber;
            set {
                _columnNumber = value;
                RaisePropertyChanged();
            }
        }

        public string Header {
            get => _header;
            set {
                _header = value;
                RaisePropertyChanged();
            }
        }

        public int Gain {
            get => _gain;
            set {
                _gain = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<FilterTiming> Settings { get; set; } = new AsyncObservableCollection<FilterTiming>();

        public void AddFilterTiming(FilterTiming filterTiming) {
            Settings.Add(filterTiming);
            RaisePropertyChanged(nameof(Settings));
        }

        public void RemoveFilterTimingByKeys(ICollection<FlatDeviceFilterSettingsKey> keys) {
            if (keys == null) return;

            var changed = false;
            foreach (var key in keys) {
                var timing = Settings.FirstOrDefault(s => Equals(s?.Key, key));
                if (timing == null) continue;
                Settings.Remove(timing);
                changed = true;
            }
            if (changed) RaisePropertyChanged(nameof(Settings));
        }

        public void RaiseChanged() {
            RaiseAllPropertiesChanged();
        }
    }

    public class FilterTiming : BaseINPC {
        private readonly IProfileService _profileService;

        public bool ShowFilterNameOnly { get; }

        public string FilterName => _profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters
                .FirstOrDefault(f => f?.Position == Key.Position)?.Name;

        public bool IsEmpty => _profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(Key) == null;

        public FlatDeviceFilterSettingsKey Key { get; }

        public FilterTiming(IProfileService profileService, FlatDeviceFilterSettingsKey key, bool showFilterNameOnly) {
            _profileService = profileService;
            Key = key;
            ShowFilterNameOnly = showFilterNameOnly;
        }

        public double Brightness {
            get => _profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(Key)?.Brightness ?? 0d;
            set {
                var temp = new FlatDeviceFilterSettingsValue(value, Time);
                _profileService.ActiveProfile.FlatDeviceSettings.AddBrightnessInfo(Key, temp);
                RaisePropertyChanged();
            }
        }

        public double Time {
            get => _profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(Key)?.Time ?? 0d;
            set {
                var temp = new FlatDeviceFilterSettingsValue(Brightness, value);
                _profileService.ActiveProfile.FlatDeviceSettings.AddBrightnessInfo(Key, temp);
                RaisePropertyChanged();
            }
        }

        public void RaiseChanged() {
            RaiseAllPropertiesChanged();
        }
    }
}