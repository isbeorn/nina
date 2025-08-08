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
using NINA.Core.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Plugin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    /// <summary>
    /// PluggableBehaviorManager exists to break a circular dependency between PluginLoader and IPluggableBehaviorSelectors. It is responsible for collecting all of the IPluggableBehaviors
    /// from the plugin loader, and then adding them to the appropriate IPluggableBehaviorSelector after plugin loading has completed
    /// </summary>
    public class PluggableBehaviorManager : IPluggableBehaviorManager {
        private readonly IPluginLoader pluginProvider;
        private readonly Dictionary<Type, IPluggableBehaviorSelector> behaviorSelectorsByType;

        public PluggableBehaviorManager(IPluggableBehaviorSelector[] pluggableBehaviorSelectors, IPluginLoader pluginProvider) {
            this.pluginProvider = pluginProvider;
            this.behaviorSelectorsByType = pluggableBehaviorSelectors.ToDictionary(s => s.GetInterfaceType());
        }

        public async Task Initialize() {
            await pluginProvider.Load();
            foreach (var pluggableBehavior in pluginProvider.PluggableBehaviors) {
                var pluggedInterfaceType = pluggableBehavior.GetType().GetInterfaces()
                    .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IPluggableBehavior<>))
                    .Select(t => t.GetGenericArguments())
                    .Where(t => t.Length > 0)
                    .Select(t => t[0])
                    .FirstOrDefault();
                if (pluggedInterfaceType == null) {
                    Logger.Warning($"PluggableBehavior {pluggedInterfaceType.FullName} implements IPluggableBehavior instead of IPluggableBehavior<T>");
                    continue;
                }

                if (!pluggedInterfaceType.IsAssignableFrom(pluggableBehavior.GetType())) {
                    Logger.Warning($"PluggableBehavior {pluggedInterfaceType.FullName} doesn't implement {pluggedInterfaceType.FullName}");
                    continue;
                }

                if (behaviorSelectorsByType.TryGetValue(pluggedInterfaceType, out var behaviorSelector)) {
                    behaviorSelector.AddBehavior(pluggableBehavior);
                } else {
                    Logger.Warning($"PluggableBehavior {pluggedInterfaceType.FullName} -> {pluggableBehavior.GetType().FullName} has no behavior selector. Ignoring");
                }
            }

            Initialized?.Invoke(this, new EventArgs());
        }

        public event EventHandler Initialized;
    }
}
