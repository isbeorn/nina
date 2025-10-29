using ASCOM.Alpaca.Discovery;
using ASCOM.Common;
using ASCOM.Common.Alpaca;
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

        private async Task<List<AscomDevice>> GetDiscoveredDevices(DeviceTypes deviceType, CancellationToken token) {
            try {
                var settings = profileService.ActiveProfile.AlpacaSettings;
                return (await AlpacaDiscovery.GetAscomDevicesAsync(deviceType,
                    numberOfPolls: settings.NumberOfPolls,
                    pollInterval: settings.PollInterval,
                    discoveryPort: settings.DiscoveryPort,
                    discoveryDuration: settings.DiscoveryDuration,
                    resolveDnsName: settings.ResolveDnsName,
                    useIpV4: settings.UseIPv4,
                    useIpV6: settings.UseIPv6,
                    serviceType: settings.UseHttps ? ServiceType.Https : ServiceType.Http,
                    cancellationToken: token)).ToList();
            } catch (OperationCanceledException) when (token.IsCancellationRequested) {
                throw;
            } catch (Exception ex) {
                Logger.Error("Alpaca discovery failed; direct-IP connections remain available.", ex);
                return new List<AscomDevice>();
            }
        }

        public async Task<List<ICamera>> GetCameras(IExposureDataFactory exposureDataFactory, CancellationToken token) {
            var l = new List<ICamera>();
            var devices = await GetDiscoveredDevices(DeviceTypes.Camera, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomCamera(device, profileService, exposureDataFactory));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectCamera(profileService, exposureDataFactory));

            return l;
        }

        public async Task<List<ITelescope>> GetTelescopes(CancellationToken token) {
            var l = new List<ITelescope>();
            var devices = await GetDiscoveredDevices(DeviceTypes.Telescope, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomTelescope(device, profileService));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectTelescope(profileService));

            return l;
        }

        public async Task<List<IFilterWheel>> GetFilterWheels(CancellationToken token) {
            var l = new List<IFilterWheel>();
            var devices = await GetDiscoveredDevices(DeviceTypes.FilterWheel, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomFilterWheel(device, profileService));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectFilterWheel(profileService));

            return l;
        }

        public async Task<List<IRotator>> GetRotators(CancellationToken token) {
            var l = new List<IRotator>();
            var devices = await GetDiscoveredDevices(DeviceTypes.Rotator, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomRotator(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectRotator(profileService));

            return l;
        }

        public async Task<List<ISafetyMonitor>> GetSafetyMonitors(CancellationToken token) {
            var l = new List<ISafetyMonitor>();
            var devices = await GetDiscoveredDevices(DeviceTypes.SafetyMonitor, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomSafetyMonitor(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectSafetyMonitor(profileService));

            return l;
        }

        public async Task<List<IFocuser>> GetFocusers(CancellationToken token) {
            var l = new List<IFocuser>();
            var devices = await GetDiscoveredDevices(DeviceTypes.Focuser, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomFocuser(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectFocuser(profileService));

            return l;
        }

        public async Task<List<ISwitchHub>> GetSwitches(CancellationToken token) {
            var l = new List<ISwitchHub>();
            var devices = await GetDiscoveredDevices(DeviceTypes.Switch, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomSwitchHub(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectSwitch(profileService));

            return l;
        }

        public async Task<List<IDome>> GetDomes(CancellationToken token) {
            var l = new List<IDome>();
            var devices = await GetDiscoveredDevices(DeviceTypes.Dome, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomDome(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectDome(profileService));

            return l;
        }

        public async Task<List<IFlatDevice>> GetCoverCalibrators(CancellationToken token) {
            var l = new List<IFlatDevice>();
            var devices = await GetDiscoveredDevices(DeviceTypes.CoverCalibrator, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomCoverCalibrator(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectCoverCalibrator(profileService));

            return l;
        }

        public async Task<List<IWeatherData>> GetWeatherDataSources(CancellationToken token) {
            var l = new List<IWeatherData>();
            var devices = await GetDiscoveredDevices(DeviceTypes.ObservingConditions, token);
            foreach (var device in devices) {
                try {
                    Logger.Info($"Discovered Alpaca Device {device.AscomDeviceName} - {device.UniqueId} @ {device.HostName} {device.IpAddress}:{device.IpPort} #{device.AlpacaDeviceNumber}");
                    l.Add(new AscomObservingConditions(device));
                } catch (Exception ex) {
                    Logger.Error("An error ocurred during creation of Alpaca Device", ex);
                }
            }

            l.Add(new AlpacaDirectObservingConditions(profileService));

            return l;
        }
    }
}
