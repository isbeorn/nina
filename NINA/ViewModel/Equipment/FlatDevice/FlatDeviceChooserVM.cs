using System;
using System.Collections.Generic;
using System.Linq;
using NINA.Model;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;

namespace NINA.ViewModel.Equipment.FlatDevice {

    internal class FlatDeviceChooserVM : EquipmentChooserVM {

        public FlatDeviceChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblFlatDeviceNoDevice"]));

            try {
                Logger.Trace("Adding Alnitak Flat Devices");
                List<string> alnitakList = AlnitakDevices.GetDevices();

                if (alnitakList.Count > 0) {
                    foreach (var device in alnitakList.Select(entry => new AlnitakFlatDevice(entry, profileService))
                        .Where(device => !string.IsNullOrEmpty(device.Name))) {
                        Logger.Debug($"Adding Alnitak flat device {device.Id} (as {device.Name})");
                        Devices.Add(device);
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            DetermineSelectedDevice(profileService.ActiveProfile.FlatDeviceSettings.Id);
        }
    }
}