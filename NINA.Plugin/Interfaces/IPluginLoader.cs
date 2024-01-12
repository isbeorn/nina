#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace NINA.Plugin.Interfaces {

    public interface IPluginLoader {
        IList<Assembly> Assemblies { get; }
        IDictionary<IPluginManifest, bool> Plugins { get; }
        IList<ISequenceCondition> Conditions { get; }
        IList<ISequenceContainer> Container { get; }
        IList<ISequenceItem> Items { get; }
        IList<ISequenceTrigger> Triggers { get; }
        IList<IDateTimeProvider> DateTimeProviders { get; }
        IList<IDockableVM> DockableVMs { get; }
        IList<IPluggableBehavior> PluggableBehaviors { get; }
        IList<IEquipmentProvider> DeviceProviders { get; }

        Task Load();
    }
}