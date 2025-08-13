#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    public class PluggableAutoFocusVMFactory : IAutoFocusVMFactory {

        private readonly IPluggableBehaviorSelector<IAutoFocusVMFactory> autoFocusVMFactorySelector;
        public PluggableAutoFocusVMFactory(IPluggableBehaviorSelector<IAutoFocusVMFactory> autoFocusVMFactorySelector) {
            this.autoFocusVMFactorySelector = autoFocusVMFactorySelector;
        }
        
        public string Name => "HIDDEN";

        public string ContentId => GetType().FullName;

        public IAutoFocusVM Create() {
            return autoFocusVMFactorySelector.GetBehavior().Create();
        }
    }
}
