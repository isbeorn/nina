#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Sequencer {

    public class SequenceDataTemplateSelector : DataTemplateSelector {
        public DataTemplate SequenceContainer { get; set; }
        public DataTemplate DeepSkyObjectContainer { get; set; }
        public DataTemplate SequenceItem { get; set; }
        public DataTemplate SequenceTrigger { get; set; }
        public DataTemplate SequenceCondition { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if(item is SidebarEntity sidebarEntity) {
                item = sidebarEntity.Entity;
            }
            if (item is IImmutableContainer) {
                return SequenceItem;
            } else if (item is IDeepSkyObjectContainer) {
                return DeepSkyObjectContainer;
            } else if (item is ISequenceContainer) {
                return SequenceContainer;
            } else if (item is ISequenceTrigger) {
                return SequenceTrigger;
            } else if (item is ISequenceCondition) {
                return SequenceCondition;
            } else {
                return SequenceItem;
            }
        }
    }
}