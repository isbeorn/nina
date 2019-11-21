using NINA.Model.MyGuider;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Profile;
using System.Linq;

namespace NINA.ViewModel.Equipment.Guider {

    public class GuiderChooserVM : BaseVM, IGuiderChooserVM {
        private readonly ICameraMediator cameraMediator;
        private ITelescopeMediator telescopeMediator;

        public GuiderChooserVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator) : base(profileService) {
            this.cameraMediator = cameraMediator;
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
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
            Guiders.Add(new DummyGuider(profileService));
            Guiders.Add(new PHD2Guider(profileService));
            Guiders.Add(new SynchronizedPHD2Guider(profileService, cameraMediator));
            Guiders.Add(new DirectGuider(profileService, telescopeMediator));

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
                var items = (from guider in Guiders where guider.Name == name select guider).ToList();
                SelectedGuider = items.Any() ? items.First() : Guiders.First();
            }
        }
    }
}