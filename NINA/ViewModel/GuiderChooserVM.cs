using NINA.Model.MyGuider;
using NINA.Utility;
using NINA.Utility.Profile;
using NINA.ViewModel.Interfaces;
using System.Linq;

namespace NINA.ViewModel {

    public class GuiderChooserVM : BaseVM, IGuiderChooserVM {

        public GuiderChooserVM(IProfileService profileService) : base(profileService) {
            this.profileService = profileService;
            GetEquipment();
        }

        private AsyncObservableCollection<IGuider> _devices;

        public AsyncObservableCollection<IGuider> Guiders {
            get {
                if (_devices == null) {
                    _devices = new AsyncObservableCollection<IGuider>();
                }
                return _devices;
            }
            set => _devices = value;
        }

        public void GetEquipment() {
            Guiders.Add(new PHD2Guider(profileService));
            //Guiders.Add(new DummyGuider());

            DetermineSelectedDevice(profileService.ActiveProfile.GuiderSettings.GuiderName);
        }

        private IGuider _selectedGuider;

        public IGuider SelectedGuider {
            get => _selectedGuider;
            set {
                _selectedGuider = value;
                RaisePropertyChanged();
            }
        }

        public void DetermineSelectedDevice(string name) {
            if (Guiders.Count > 0) {
                var items = (from device in Guiders where device.Name == name select device).ToList();
                SelectedGuider = items.Any() ? items.First() : Guiders.First();
            }
        }
    }
}