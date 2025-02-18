using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NINA.Plugin.PluginCompatibilityMap;

namespace NINA.Plugin {
    static class PluginMergedEntityMap {
        public static Dictionary<string, List<string>> MergedEntities => new Dictionary<string, List<string>>() {
            {
                "Connector",
                new List<string>() {
                    "NINA.Plugins.Connector.Instructions.ConnectAllEquipment",
                    "NINA.Plugins.Connector.Instructions.ConnectEquipment",
                    "NINA.Plugins.Connector.Instructions.DisconnectAllEquipment",
                    "NINA.Plugins.Connector.Instructions.DisconnectEquipment",
                    "NINA.Plugins.Connector.Instructions.ReconnectOnDownloadFailure",
                    "NINA.Plugins.Connector.Instructions.ReconnectTrigger",
                    "NINA.Plugins.Connector.Instructions.SwitchProfile"

                }
            }
        };
    }
}
