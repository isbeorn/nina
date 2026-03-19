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
using System.Text.RegularExpressions;
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
            var cxUsbCameras = GetCameras(() => new MoravianCxUsbSdk(), MoravianCxUsbSdk.Scan());
            foreach(var camera in cxUsbCameras) {
                devices.Add(new MoravianCamera(cameraId: camera.Id,
                                serialNumber: camera.SerialNumber,
                                name: camera.Name,
                                category: camera.Category,
                                driverVersion: camera.DriverVersion,
                                firmwareVersion: camera.FirmwareVersion,
                                flashVersion: camera.FlashVersion,
                                sdk: camera.Sdk,
                                profileService: profileService,
                                exposureDataFactory: exposureDataFactory));

            }

            Logger.Debug("Scanning for Moravian Gx USB Cameras");
            var gxUsbCameras = GetCameras(() => new MoravianGxUsbSdk(), MoravianGxUsbSdk.Scan());
            foreach (var camera in gxUsbCameras) {
                devices.Add(new MoravianCamera(cameraId: camera.Id,
                                serialNumber: camera.SerialNumber,
                                name: camera.Name,
                                category: camera.Category,
                                driverVersion: camera.DriverVersion,
                                firmwareVersion: camera.FirmwareVersion,
                                flashVersion: camera.FlashVersion,
                                sdk: camera.Sdk,
                                profileService: profileService,
                                exposureDataFactory: exposureDataFactory));

            }

            Logger.Debug("Scanning for Moravian Gx Ethernet Cameras");
            var gxEthCameras = GetCameras(() => new MoravianGxEthSdk(), MoravianGxEthSdk.Scan());
            foreach (var camera in gxEthCameras) {
                devices.Add(new MoravianCamera(cameraId: camera.Id,
                                serialNumber: camera.SerialNumber,
                                name: camera.Name,
                                category: camera.Category,
                                driverVersion: camera.DriverVersion,
                                firmwareVersion: camera.FirmwareVersion,
                                flashVersion: camera.FlashVersion,
                                sdk: camera.Sdk,
                                profileService: profileService,
                                exposureDataFactory: exposureDataFactory));

            }

            Logger.Info($"Found {devices.Count} Moravian Cameras");
            return devices;
        }


        private static Lock scanLock = new Lock();
        public static IList<MoravianCameraInfo> GetCameras(Func<IMoravianCameraSDK> sdkFactory, List<uint> cameraIds) {
            using var scope = scanLock.EnterScope();
            var devices = new List<MoravianCameraInfo>();
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
                    if (!sdk.GetBooleanParameter(handle, MoravianBooleanParameter.gbpFilters, out bool hasFilterWheel)) {
                        Logger.Warning($"Moravian SDK: Could not GetBooleanParameter gbpFilters for camera id {id} to query its integrated filter wheel");
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

                    devices.Add(new MoravianCameraInfo(Id: id,
                                                  SerialNumber: cameraSerial.ToString(),
                                                  Name: Regex.Replace(cameraName.ToString(), @"\s+", " "),
                                                  Category: "Moravian Instruments",
                                                  DriverVersion: $"{driverMajor}.{driverMinor}.{driverBuild}",
                                                  FirmwareVersion: $"{firmwareMajor}.{firmwareMinor}.{firmwareBuild}",
                                                  FlashVersion: $"{flashMajor}.{flashMinor}.{flashBuild}",
                                                  HasFilterWheel: hasFilterWheel,
                                                  Sdk: sdk));
                } catch (Exception ex) {
                    Logger.Error($"Moravian SDK: failed to get camera for camera id {id}", ex);
                }
            }
            return devices;
        }

        public record MoravianCameraInfo(uint Id, string SerialNumber, string Name, string Category, string DriverVersion, string FirmwareVersion, string FlashVersion, bool HasFilterWheel, IMoravianCameraSDK Sdk);
    }
}
