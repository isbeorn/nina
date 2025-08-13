using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Plugin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    public interface IPluginEquipmentProviderManager {
        Task Initialize();
    }
    public class PluginEquipmentProviderManager : IPluginEquipmentProviderManager {
        private readonly IPluginLoader pluginProvider;
        private readonly Dictionary<Type, IEquipmentProviders> equipmentProviders;

        public PluginEquipmentProviderManager(IEquipmentProviders[] equipmentProviders, IPluginLoader pluginProvider) {
            this.pluginProvider = pluginProvider;
            this.equipmentProviders = equipmentProviders.ToDictionary(s => s.GetInterfaceType());
        }

        public async Task Initialize() {
            await pluginProvider.Load();
            foreach (var deviceProvider in pluginProvider.DeviceProviders) {
                var pluggedDevice = deviceProvider.GetType().GetInterfaces()
                    .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEquipmentProvider<>))
                    .Select(t => t.GetGenericArguments())
                    .Where(t => t.Length > 0)
                    .Select(t => t[0])
                    .FirstOrDefault();
                if (pluggedDevice == null) {
                    Logger.Warning($"EquipmentProvider {pluggedDevice.FullName} implements IEquipmentProvider instead of IEquipmentProvider<T>");
                    continue;
                }

                if(deviceProvider.GetType().GetInterfaces().Any(x => x.IsGenericType && x.GetGenericArguments().Any(g => g == pluggedDevice))) {
                    if(equipmentProviders.TryGetValue(pluggedDevice, out var behaviorSelector)) {
                        behaviorSelector.AddProvider(deviceProvider);
                    }
                }
            }

            foreach(var provider in equipmentProviders) {
                provider.Value.Initialized = true;
            }
        }
    }

    public class PluginEquipmentProviders<T> : IEquipmentProviders<T> where T : IDevice {

        private IList<IEquipmentProvider<T>> providers;

        public PluginEquipmentProviders() {
            providers = new List<IEquipmentProvider<T>>();
        }

        public bool Initialized { get; set; }


        public void AddProvider(IEquipmentProvider deviceProvider) {
            var specific = deviceProvider as IEquipmentProvider<T>;
            if(specific == null) {
                throw new ArgumentException($"Can't add device provider {deviceProvider.GetType().FullName} since it doesn't implement {typeof(T).FullName}");
            }
            providers.Add(specific);
        }

        public Type GetInterfaceType() {
            return typeof(T);
        }

        public async Task<IList<IEquipmentProvider<T>>> GetProviders() {
            while(!Initialized) {
                await Task.Delay(10);
            }
            return providers;
        }
    }
}
