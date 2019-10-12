using NINA.Model.MyGuider;
using NINA.Utility;

namespace NINA.ViewModel.Equipment.Guider {

    public interface IGuiderChooserVM {
        AsyncObservableCollection<IGuider> Guiders { get; set; }
        IGuider SelectedGuider { get; set; }
    }
}