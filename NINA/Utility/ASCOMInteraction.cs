using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {

    internal class ASCOMInteraction {

        public static List<ICamera> GetCameras(IProfileService profileService) {
            var l = new List<ICamera>();
            var ascomDevices = new ASCOM.Utilities.Profile();
            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Camera")) {
                try {
                    AscomCamera cam = new AscomCamera(device.Key, device.Value + " (ASCOM)", profileService);
                    Logger.Trace(string.Format("Adding {0}", cam.Name));
                    l.Add(cam);
                } catch (Exception) {
                    //only add cameras which are supported. e.g. x86 drivers will not work in x64
                }
            }
            return l;
        }

        public static List<ITelescope> GetTelescopes(IProfileService profileService) {
            var l = new List<ITelescope>();
            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Telescope")) {
                try {
                    AscomTelescope telescope = new AscomTelescope(device.Key, device.Value, profileService);
                    l.Add(telescope);
                } catch (Exception) {
                    //only add telescopes which are supported. e.g. x86 drivers will not work in x64
                }
            }
            return l;
        }

        public static List<IFilterWheel> GetFilterWheels(IProfileService profileService) {
            var l = new List<IFilterWheel>();
            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("FilterWheel")) {
                try {
                    AscomFilterWheel fw = new AscomFilterWheel(device.Key, device.Value);
                    l.Add(fw);
                } catch (Exception) {
                    //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                }
            }
            return l;
        }

        public static List<IRotator> GetRotators(IProfileService profileService) {
            var l = new List<IRotator>();
            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Rotator")) {
                try {
                    AscomRotator rotator = new AscomRotator(device.Key, device.Value);
                    l.Add(rotator);
                } catch (Exception) {
                    //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                }
            }
            return l;
        }

        public static List<IFocuser> GetFocusers(IProfileService profileService) {
            var l = new List<IFocuser>();
            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Focuser")) {
                try {
                    AscomFocuser focuser = new AscomFocuser(device.Key, device.Value);
                    l.Add(focuser);
                } catch (Exception) {
                    //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                }
            }
            return l;
        }
    }
}