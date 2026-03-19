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
using System.Linq;
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
                // When the camera is already connected and has an integrated filter wheel we grab the info from the instance directly
                devices.Add(new MoravianIntegratedFilterWheel(profileService: profileService,
                                                              cameraMediator: cameraMediator,
                                                              serialNumber: mc.SerialNumber,
                                                              name: mc.Name,
                                                              category: mc.Category,
                                                              driverVersion: mc.DriverVersion,
                                                              firmwareVersion: mc.FirmwareVersion,
                                                              flashVersion: mc.FlashVersion));
            }

            // For all other cases we scan for cameras via the camera provider and check if there have filter wheels
            Logger.Debug("Scanning for Moravian Cx USB Camera Filter Wheels");
            var cxUsbCameras = MoravianCameraProvider.GetCameras(() => new MoravianCxUsbSdk(), MoravianCxUsbSdk.Scan());
            foreach(var camera in cxUsbCameras.Where(x => x.HasFilterWheel)) {
                devices.Add(new MoravianIntegratedFilterWheel(profileService: profileService,
                                                              cameraMediator: cameraMediator,
                                                              serialNumber: camera.SerialNumber,
                                                              name: camera.Name,
                                                              category: camera.Category,
                                                              driverVersion: camera.DriverVersion,
                                                              firmwareVersion: camera.FirmwareVersion,
                                                              flashVersion: camera.FlashVersion));
            }

            Logger.Debug("Scanning for Moravian Gx USB Camera Filter Wheels");
            var gxUsbCameras = MoravianCameraProvider.GetCameras(() => new MoravianGxUsbSdk(), MoravianGxUsbSdk.Scan());
            foreach (var camera in gxUsbCameras.Where(x => x.HasFilterWheel)) {
                devices.Add(new MoravianIntegratedFilterWheel(profileService: profileService,
                                                              cameraMediator: cameraMediator,
                                                              serialNumber: camera.SerialNumber,
                                                              name: camera.Name,
                                                              category: camera.Category,
                                                              driverVersion: camera.DriverVersion,
                                                              firmwareVersion: camera.FirmwareVersion,
                                                              flashVersion: camera.FlashVersion));
            }

            Logger.Debug("Scanning for Moravian Gx Ethernet Camera Filter Wheels");
            var gxEthCameras = MoravianCameraProvider.GetCameras(() => new MoravianGxEthSdk(), MoravianGxEthSdk.Scan());
            foreach (var camera in gxEthCameras.Where(x => x.HasFilterWheel)) {
                devices.Add(new MoravianIntegratedFilterWheel(profileService: profileService,
                                                              cameraMediator: cameraMediator,
                                                              serialNumber: camera.SerialNumber,
                                                              name: camera.Name,
                                                              category: camera.Category,
                                                              driverVersion: camera.DriverVersion,
                                                              firmwareVersion: camera.FirmwareVersion,
                                                              flashVersion: camera.FlashVersion));
            }

            Logger.Info($"Found {devices.Count} Moravian Integrated Filter Wheels");
            return devices;
        }
    }
}
