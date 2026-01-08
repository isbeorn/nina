using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;

namespace MoravianCameraSDK {
    [Export(typeof(IEquipmentProvider))]
    public class MoravianCameraProvider : IEquipmentProvider<ICamera> {

        public string Name => "Moravian Instruments";
        public string ContentId => this.GetType().FullName;
        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;

        [ImportingConstructor]
        public MoravianCameraProvider(IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
        }

        public IList<ICamera> GetEquipment() {
            var devices = new List<ICamera>();

            Logger.Debug("Scanning for Moravian Cx USB Cameras");
            devices.AddRange(GetCameras(() => new MoravianCxUsbSdk(), MoravianCxUsbSdk.Scan()));

            Logger.Debug("Scanning for Moravian Gx USB Cameras");
            devices.AddRange(GetCameras(() => new MoravianGxUsbSdk(), MoravianGxUsbSdk.Scan()));

            Logger.Debug("Scanning for Moravian Gx Ethernet Cameras");
            devices.AddRange(GetCameras(() => new MoravianGxEthSdk(), MoravianGxEthSdk.Scan()));

            return devices;
        }


        public static Lock ScanLock = new Lock();
        private IList<ICamera> GetCameras(Func<IMoravianCameraSDK> sdkFactory, List<uint> cameraIds) {
            using var scope = ScanLock.EnterScope();
            var devices = new List<ICamera>();
            foreach (var id in cameraIds) {
                try {
                    var sdk = sdkFactory();
                    UIntPtr handle = sdk.Initialize(id);
                    if (handle == UIntPtr.Zero) {
                        Logger.Warning($"Moravian SDK: Could not initialize camera id {id}");
                        sdk.Release(handle);
                        continue;
                    }

                    StringBuilder cameraName = new StringBuilder(256);
                    if (!sdk.GetStringParameter(handle, MoravianStringParameter.gspCameraDescription, byte.MaxValue, cameraName)) {
                        Logger.Warning($"Moravian SDK: Could not get camera description for camera id {id}");
                        sdk.Release(handle);
                        continue;
                    }
                    StringBuilder cameraSerial = new StringBuilder(256);
                    if (!sdk.GetStringParameter(handle, MoravianStringParameter.gspCameraSerial, byte.MaxValue, cameraSerial)) {
                        Logger.Warning($"Moravian SDK: Could not get camera serial for camera id {id}");
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

                    devices.Add(new MoravianCamera(cameraId: id,
                                                  id: $"Moravian-{cameraSerial}",
                                                  name: cameraName.ToString(),
                                                  category: "Moravian Instruments",
                                                  driverVersion: $"{driverMajor}.{driverMinor}.{driverBuild}",
                                                  firmwareVersion: $"{firmwareMajor}.{firmwareMinor}.{firmwareBuild}",
                                                  flashVersion: $"{flashMajor}.{flashMinor}.{flashBuild}",
                                                  sdk: sdk,
                                                  profileService: profileService,
                                                  exposureDataFactory: exposureDataFactory));
                } catch (Exception ex) {
                    Logger.Error($"Moravian SDK: failed to get camera for camera id {id}", ex);
                }
            }
            return devices;
        }
    }
}
