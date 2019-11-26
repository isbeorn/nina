using System.Threading.Tasks;
using NINA.Model.MyFlatDevice;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.FlatDevice;

namespace NINA.Utility.Mediator {

    internal class FlatDeviceMediator : DeviceMediator<IFlatDeviceVM, IFlatDeviceConsumer, FlatDeviceInfo>, IFlatDeviceMediator {

        public void SetBrightness(double brightness) {
            handler.Brightness = brightness;
            handler.SetBrightness(null);
        }

        public Task CloseCover() {
            return handler.CloseCover();
        }

        public void ToggleLight(object o) {
            handler.ToggleLight(o);
        }

        public Task OpenCover() {
            return handler.OpenCover();
        }
    }
}