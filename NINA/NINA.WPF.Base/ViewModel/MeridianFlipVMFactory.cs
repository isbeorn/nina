#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.WPF.Base.ViewModel {

    public class MeridianFlipVMFactory : IMeridianFlipVMFactory {
        private readonly IProfileService profileService;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IDomeFollower domeFollower;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IImageHistoryVM history;
        private readonly IAutoFocusVMFactory autoFocusVMFactory;

        public MeridianFlipVMFactory(
                IProfileService profileService,
                ITelescopeMediator telescopeMediator,
                IGuiderMediator guiderMediator,
                IImagingMediator imagingMediator,
                IDomeMediator domeMediator,
                IDomeFollower domeFollower,
                IApplicationStatusMediator applicationStatusMediator,
                IFilterWheelMediator filterWheelMediator,
                IImageHistoryVM history,
                IAutoFocusVMFactory autoFocusVMFactory) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.applicationStatusMediator = applicationStatusMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.history = history;
            this.autoFocusVMFactory = autoFocusVMFactory;
        }

        public IMeridianFlipVM Create() {
            return new MeridianFlipVM(profileService, telescopeMediator, guiderMediator, imagingMediator, domeMediator, domeFollower, applicationStatusMediator, filterWheelMediator, history, autoFocusVMFactory);
        }
    }
}