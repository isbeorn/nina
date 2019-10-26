using NINA.Model.MyFlatDevice;
using NINA.ViewModel.Equipment.FlatDevice;

namespace NINA.Utility.Mediator.Interfaces {

    public interface IFlatDeviceMediator : IDeviceMediator<IFlatDeviceVM, IFlatDeviceConsumer, FlatDeviceInfo> {
    }
}