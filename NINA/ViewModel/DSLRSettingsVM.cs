using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Utility.Enum;
using NINA.Utility.Profile;

namespace NINA.ViewModel {

    internal class DSLRSettingsVM : BaseVM {

        public DSLRSettingsVM(IProfileService profileService) : base(profileService) {
            DSLRFileSaveMode = DSLRFileSaveMode.RAW;
        }

        private DSLRFileSaveMode dSLRFileSaveMode;

        public DSLRFileSaveMode DSLRFileSaveMode {
            get {
                return dSLRFileSaveMode;
            }
            set {
                dSLRFileSaveMode = value;
                RaisePropertyChanged();
            }
        }
    }
}