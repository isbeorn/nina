using System.Threading.Tasks;
using NINA.Model.MyFlatDevice;
using NINA.ViewModel.Equipment.FlatDevice;

namespace NINA.Utility.Mediator.Interfaces {

    public interface IFlatDeviceMediator : IDeviceMediator<IFlatDeviceVM, IFlatDeviceConsumer, FlatDeviceInfo> {

        void SetBrightness(double brightness);

        Task CloseCover();

        void ToggleLight(object o);

        Task OpenCover();
    }
}