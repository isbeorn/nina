#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
using NINA.Sequencer.Utility;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NINA.Sequencer {

    public abstract class SequenceEntityINPC : BaseINPC {

        public const string defaultChangeSet = "*";

        public SequenceEntityINPC() {
            PropertyChanged += OnPropertyChanged;
        }

        private Dictionary<string, System.Reflection.PropertyInfo> propertyInfoByname = new Dictionary<string, System.Reflection.PropertyInfo>();
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            System.Reflection.PropertyInfo propInf;
            if (e?.PropertyName == null) { return; }
            if (propertyInfoByname.ContainsKey(e.PropertyName)) {
                propInf = propertyInfoByname[e.PropertyName];
            } else {
                propInf = GetType().GetProperty(e.PropertyName);
                propertyInfoByname.Add(e.PropertyName, propInf);
            }

            if (propInf?.GetCustomAttributes(typeof(JsonPropertyAttribute), true)?.Length > 0) {
                ISequenceRootContainer root = GetSequenceRootContainer();

                if (root != null && !(root.HasChanges?[defaultChangeSet] ?? false)) { 
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
                        root?.HasChanges?[defaultChangeSet] = true;
                    }
                }
            }
        }


        public virtual ISequenceRootContainer GetSequenceRootContainer() {
            return ItemUtility.GetRootContainer(((ISequenceEntity)this).Parent);
        }
    }
}
