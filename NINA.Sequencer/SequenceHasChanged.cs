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
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NINA.Sequencer {

    public abstract class SequenceHasChanged : BaseINPC, ISequenceHasChanged {

        public SequenceHasChanged() {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            //var _profileService = (ProfileService)Application.Current.Resources["ProfileService"];
            //Trace.WriteLine(_profileService.ActiveProfile.ImageFileSettings.FilePath);

            ISequenceRootContainer root = GetSequenceRootContainer();

            if ((root!=null) && (!root.HasChanges["*"])) {
                System.Reflection.PropertyInfo propInf = GetType().GetProperty(e.PropertyName);
                if (propInf.GetCustomAttributes(typeof(JsonPropertyAttribute), true).Length > 0) {
                    object[] hasChangedSets = propInf.GetCustomAttributes(typeof(NINA.Core.Model.HasChangedSetAttribute), true);
                    if (hasChangedSets.Length > 0) {
                        foreach (object item in hasChangedSets) {
                            HasChangedSetAttribute att = (HasChangedSetAttribute)item;
                                if (root.HasChanges.ContainsKey(att.HasChangedSet)) {
                                root.HasChanges[att.HasChangedSet] = true;
                                } else {
                                root.HasChanges.Add(att.HasChangedSet, true);
                                }
                        }
                    } else {
                        root.HasChanges["*"] = true;
                    }
                }
            }
        }


        public virtual ISequenceRootContainer GetSequenceRootContainer() {
            if (this is ISequenceEntity) {
                ISequenceEntity item = ((ISequenceEntity)this);
                while (item.Parent != null) {
                    if (item.Parent is ISequenceRootContainer)
                        return item.Parent as ISequenceRootContainer;
                    item = item.Parent;
                }
            }
            return null;
        }
    }
}