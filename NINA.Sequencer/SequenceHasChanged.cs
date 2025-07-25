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
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;

namespace NINA.Sequencer {

    public abstract class SequenceHasChanged : BaseINPC, ISequenceHasChanged {

        public SequenceHasChanged() {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            //var _profileService = (ProfileService)Application.Current.Resources["ProfileService"];
            //Trace.WriteLine(_profileService.ActiveProfile.ImageFileSettings.FilePath);

            if (!HasThisItemChanged) {
                System.Reflection.PropertyInfo propInf = GetType().GetProperty(e.PropertyName);
                if (propInf.GetCustomAttributes(typeof(JsonPropertyAttribute), true).Length > 0) {
                    object[] hasChangedSets = propInf.GetCustomAttributes(typeof(NINA.Core.Model.HasChangedSetAttribute), true);
                    if (hasChangedSets.Length > 0) {
                        foreach (object item in hasChangedSets) {
                            HasChangedSetAttribute att = (HasChangedSetAttribute)item;
                                if (hasChangedBySet.ContainsKey(att.HasChangedSet)) {
                                    hasChangedBySet[att.HasChangedSet] = true;
                                } else {
                                    hasChangedBySet.Add(att.HasChangedSet, true);
                                }
                        }
                    } else {
                        hasChangedBySet["*"] = true;
                    }
                }
            }
        }

        // just check this item, container items have an overridden HasChanged to check children
        public bool HasThisItemChanged {
            get => hasChangedBySet["*"];
        }

        public virtual bool HasChanged {
            get => HasChangedBySet.ContainsKey("*") && HasChangedBySet["*"];
            set => HasChangedBySet["*"] = value;
        }

        private Dictionary<string, bool> hasChangedBySet = new Dictionary<string, bool>() { { "*", false } };
        public virtual Dictionary<string, bool> HasChangedBySet {
            get => hasChangedBySet;
            set => hasChangedBySet = value;
        }

        public virtual void ClearHasChanged() {
            foreach (string key in hasChangedBySet.Keys) {
                hasChangedBySet[key] = false;
            }
        }

/*        public bool AskHasChanged(string name) {
            if (HasChanged &&
                MyMessageBox.Show(string.Format(Loc.Instance["LblChangedSequenceWarning"], name ?? ""), Loc.Instance["LblChangedSequenceWarningTitle"], MessageBoxButton.YesNo, MessageBoxResult.Yes) == MessageBoxResult.No) {
                return true;
            }
            return false;
        }
*/
        public bool AskHasChanged(string name, string hasChangedSet) {
            if ((HasChangedBySet.ContainsKey(hasChangedSet)) && (HasChangedBySet[hasChangedSet]) &&
                (MyMessageBox.Show(
                    string.Format(Loc.Instance["LblChangedSequenceWarning"], name ?? ""), 
                    Loc.Instance["LblChangedSequenceWarningTitle"], 
                    MessageBoxButton.YesNo, MessageBoxResult.Yes) == MessageBoxResult.No)) {
                return true;
            }
            return false;
        }
    }
}