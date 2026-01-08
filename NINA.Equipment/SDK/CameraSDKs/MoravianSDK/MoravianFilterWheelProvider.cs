using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;

namespace MoravianCameraSDK {
    [Export(typeof(IEquipmentProvider))]
    public class MoravianFilterWheelProvider : IEquipmentProvider<IFilterWheel> {
        public string Name => "Moravian Instruments";
        public string ContentId => this.GetType().FullName;
        private readonly IProfileService profileService;
        private readonly ICameraMediator cameraMediator;

        [ImportingConstructor]
        public MoravianFilterWheelProvider(IProfileService profileService, ICameraMediator cameraMediator) {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
        }
        public IList<IFilterWheel> GetEquipment() {
            var devices = new List<IFilterWheel>();

            if (cameraMediator.GetDevice() is MoravianCamera mc && mc.HasFilterWheel()) {
                // When the camera is connected the scan won't return it, so we grab the info from the instance
                devices.Add(new MoravianIntegratedFilterWheel(profileService, cameraMediator, mc.Id.Replace(mc.Category + "_", ""), mc.Name, mc.Category, mc.DriverVersion, mc.FirmwareVersion, mc.FlashVersion));
            } else {
                // When not connected we need the Moravian Camera instances
                Logger.Debug("Scanning for Moravian Cx USB Camera Filter Wheels");
                devices.AddRange(GetFilterWheels(() => new MoravianCxUsbSdk(), MoravianCxUsbSdk.Scan()));

                Logger.Debug("Scanning for Moravian Gx USB Camera Filter Wheels");
                devices.AddRange(GetFilterWheels(() => new MoravianGxUsbSdk(), MoravianGxUsbSdk.Scan()));

                Logger.Debug("Scanning for Moravian Gx Ethernet Camera Filter Wheels");
                devices.AddRange(GetFilterWheels(() => new MoravianGxEthSdk(), MoravianGxEthSdk.Scan()));
            }

            return devices;
        }
        private IList<IFilterWheel> GetFilterWheels(Func<IMoravianCameraSDK> sdkFactory, List<uint> cameraIds) {
            // A bit ugly but we need to ensure to not stomp each other when camera thread is scanning in parallel
            using var scope = MoravianCameraProvider.ScanLock.EnterScope();
            var devices = new List<IFilterWheel>();
            foreach (var id in cameraIds) {
                try {
                    var sdk = sdkFactory();
                    UIntPtr handle = sdk.Initialize(id);
                    if (handle == UIntPtr.Zero) {
                        Logger.Warning($"Moravian SDK: Could not initialize camera id {id} to query its integrated filter wheel");
                        continue;
                    }

                    if (!sdk.GetBooleanParameter(handle, MoravianBooleanParameter.gbpFilters, out bool hasFilterWheel)) {
                        Logger.Warning($"Moravian SDK: Could not GetBooleanParameter gbpFilters for camera id {id} to query its integrated filter wheel");
                        sdk.Release(handle);
                        continue;
                    }

                    if (!hasFilterWheel) {
                        sdk.Release(handle);
                        continue;
                    }

                    StringBuilder cameraName = new StringBuilder(256);
                    if (!sdk.GetStringParameter(handle, MoravianStringParameter.gspCameraDescription, byte.MaxValue, cameraName)) {
                        Logger.Warning($"Moravian SDK: Could not get camera description for camera id {id} to query its integrated filter wheel");
                        sdk.Release(handle);
                        continue;
                    }
                    StringBuilder cameraSerial = new StringBuilder(256);
                    if (!sdk.GetStringParameter(handle, MoravianStringParameter.gspCameraSerial, byte.MaxValue, cameraSerial)) {
                        Logger.Warning($"Moravian SDK: Could not get camera serial for camera id {id} to query its integrated filter wheel");
                        sdk.Release(handle);
                        continue;
                    }

                    sdk.GetIntegerParameter(handle, MoravianIntegerParameter.gipDriverMajor, out int driverMajor);
                    sdk.GetIntegerParameter(handle, MoravianIntegerParameter.gipDriverMinor, out int driverMinor);
                    sdk.GetIntegerParameter(handle, MoravianIntegerParameter.gipDriverBuild, out int driverBuild);

                    sdk.GetIntegerParameter(handle, MoravianIntegerParameter.gipFlashMajor, out int flashMajor);
                    sdk.GetIntegerParameter(handle, MoravianIntegerParameter.gipFlashMinor, out int flashMinor);
                    sdk.GetIntegerParameter(handle, MoravianIntegerParameter.gipFlashBuild, out int flashBuild);

                    sdk.GetIntegerParameter(handle, MoravianIntegerParameter.gipFirmwareMajor, out int firmwareMajor);
                    sdk.GetIntegerParameter(handle, MoravianIntegerParameter.gipFirmwareMinor, out int firmwareMinor);
                    sdk.GetIntegerParameter(handle, MoravianIntegerParameter.gipFirmwareBuild, out int firmwareBuild);

                    sdk.Release(handle);

                    devices.Add(new MoravianIntegratedFilterWheel(id: $"Moravian-{cameraSerial}",
                                                  profileService: profileService,
                                                  cameraMediator: cameraMediator,
                                                  name: cameraName.ToString(),
                                                  category: "Moravian Instruments",
                                                  driverVersion: $"{driverMajor}.{driverMinor}.{driverBuild}",
                                                  firmwareVersion: $"{firmwareMajor}.{firmwareMinor}.{firmwareBuild}",
                                                  flashVersion: $"{flashMajor}.{flashMinor}.{flashBuild}"));
                } catch (Exception ex) {
                    Logger.Error($"Moravian SDK: failed to get camera for camera id {id} to query its integrated filter wheel", ex);
                }
            }
            return devices;
        }
    }
}
