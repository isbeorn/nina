#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Core.MyMessageBox;
using NINA.Core.Locale;
using System.Windows;
using System.ComponentModel;

namespace NINA.Sequencer {

    public abstract class SequenceHasChanged : BaseINPC, ISequenceHasChanged {

        public SequenceHasChanged() {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (!hasChanged &&
                GetType().GetProperty(e.PropertyName).GetCustomAttributes(typeof(JsonPropertyAttribute), true).Length > 0) {
                hasChanged = true;
            }
        }

        private bool hasChanged { get; set; }

        public virtual bool HasChanged {
            get => hasChanged;
            set => hasChanged = value;
        }

        public virtual void ClearHasChanged() {
            hasChanged = false;
        }

        public bool AskHasChanged(string name) {
            if (HasChanged &&
                MyMessageBox.Show(string.Format(Loc.Instance["LblChangedSequenceWarning"], name ?? ""), Loc.Instance["LblChangedSequenceWarningTitle"], MessageBoxButton.YesNo, MessageBoxResult.Yes) == MessageBoxResult.No) {
                return true;
            }
            return false;
        }
    }
}