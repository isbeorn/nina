using NINA.Model.MyFlatDevice;
using System.Threading.Tasks;
using NINA.Utility;

namespace NINA.ViewModel.Equipment.FlatDevice {

    public interface IFlatDeviceVM : IDeviceVM<FlatDeviceInfo> {

        Task<bool> OpenCover();

        Task<bool> CloseCover();

        double Brightness { get; set; }
        bool LightOn { get; set; }
        FlatDeviceInfo FlatDeviceInfo { get; set; }

        RelayCommand ToggleLightCommand { get; }
        RelayCommand SetBrightnessCommand { get; }
    }
}