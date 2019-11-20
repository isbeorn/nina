using NINA.Model;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace NINA.ViewModel.Equipment.FlatDevice {

    public interface IFlatDeviceChooserVM {
        IDevice SelectedDevice { get; set; }

        void GetEquipment();
    }

    internal class FlatDeviceChooserVM : EquipmentChooserVM, IFlatDeviceChooserVM {

        public FlatDeviceChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblFlatDeviceNoDevice"]));

            Logger.Trace("Adding Alnitak Flat Devices");
            Devices.Add(new AlnitakFlipFlatSimulator(profileService));
            Devices.Add(new AlnitakFlatDevice(profileService));
            DetermineSelectedDevice(profileService.ActiveProfile.FlatDeviceSettings.Id);
        }
    }
}