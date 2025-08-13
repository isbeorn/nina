using ASCOM.Alpaca.Discovery;
using ASCOM.Common;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch.Ascom;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Utility {
    public class AlpacaInteraction {
        private readonly IProfileService profileService;        

        public AlpacaInteraction(IProfileService profileService) {
            this.profileService = profileService;
            
        }

        public async Task<List<ICamera>> GetCameras(IExposureDataFactory exposureDataFactory, CancellationToken token) {
            var l = new List<ICamera>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.Camera,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomCamera(device, profileService, exposureDataFactory));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }

        public async Task<List<ITelescope>> GetTelescopes(CancellationToken token) {
            var l = new List<ITelescope>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.Telescope,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomTelescope(device, profileService));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }

        public async Task<List<IFilterWheel>> GetFilterWheels(CancellationToken token) {
            var l = new List<IFilterWheel>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.FilterWheel,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomFilterWheel(device, profileService));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }

        public async Task<List<IRotator>> GetRotators(CancellationToken token) {
            var l = new List<IRotator>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.Rotator,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomRotator(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }

        public async Task<List<ISafetyMonitor>> GetSafetyMonitors(CancellationToken token) {
            var l = new List<ISafetyMonitor>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.SafetyMonitor,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomSafetyMonitor(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }

        public async Task<List<IFocuser>> GetFocusers(CancellationToken token) {
            var l = new List<IFocuser>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.Focuser,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomFocuser(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }

        public async Task<List<ISwitchHub>> GetSwitches(CancellationToken token) {
            var l = new List<ISwitchHub>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.Switch,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomSwitchHub(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }

        public async Task<List<IDome>> GetDomes(CancellationToken token) {
            var l = new List<IDome>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.Dome,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomDome(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }

        public async Task<List<IFlatDevice>> GetCoverCalibrators(CancellationToken token) {
            var l = new List<IFlatDevice>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.CoverCalibrator,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomCoverCalibrator(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }

        public async Task<List<IWeatherData>> GetWeatherDataSources(CancellationToken token) {
            var l = new List<IWeatherData>();
            var devices = await AlpacaDiscovery.GetAscomDevicesAsync(DeviceTypes.ObservingConditions,
                                                                     numberOfPolls: profileService.ActiveProfile.AlpacaSettings.NumberOfPolls,
                                                                     pollInterval: profileService.ActiveProfile.AlpacaSettings.PollInterval,
                                                                     discoveryPort: profileService.ActiveProfile.AlpacaSettings.DiscoveryPort,
                                                                     discoveryDuration: profileService.ActiveProfile.AlpacaSettings.DiscoveryDuration,
                                                                     resolveDnsName: profileService.ActiveProfile.AlpacaSettings.ResolveDnsName,
                                                                     useIpV4: profileService.ActiveProfile.AlpacaSettings.UseIPv4,
                                                                     useIpV6: profileService.ActiveProfile.AlpacaSettings.UseIPv6,
                                                                     serviceType: profileService.ActiveProfile.AlpacaSettings.UseHttps ? ASCOM.Common.Alpaca.ServiceType.Https : ASCOM.Common.Alpaca.ServiceType.Http,
                                                                     cancellationToken: token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomObservingConditions(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            return l;
        }
    }
}
