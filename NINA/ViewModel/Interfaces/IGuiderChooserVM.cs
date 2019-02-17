using NINA.Model.MyGuider;
using NINA.Utility;

namespace NINA.ViewModel.Interfaces {

    public interface IGuiderChooserVM {
        AsyncObservableCollection<IGuider> Guiders { get; set; }
        IGuider SelectedGuider { get; set; }
    }
}