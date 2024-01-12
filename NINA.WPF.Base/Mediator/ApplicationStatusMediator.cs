#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.WPF.Base.Mediator {

    public class ApplicationStatusMediator : IApplicationStatusMediator {
        protected IApplicationStatusVM handler;

        public void RegisterHandler(IApplicationStatusVM handler) {
            if (this.handler != null) {
                throw new Exception("Handler already registered!");
            }
            this.handler = handler;
        }

        public void StatusUpdate(ApplicationStatus status) {
            handler.StatusUpdate(status);
        }
    }
}