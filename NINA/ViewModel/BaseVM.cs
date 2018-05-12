using NINA.Utility;
using NINA.Utility.Profile;

namespace NINA.ViewModel {

    public class BaseVM : BaseINPC {

        public BaseVM(IProfileService profileService) {
            this.profileService = profileService;
        }

        protected IProfileService profileService;
    }
}