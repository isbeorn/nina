#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility.WindowService;
using NINA.WPF.Base.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IMeridianFlipVM {
        ICommand CancelCommand { get; set; }
        TimeSpan RemainingTime { get; set; }
        ApplicationStatus Status { get; set; }
        AutomatedWorkflow Steps { get; set; }
        IWindowServiceFactory WindowServiceFactory { get; set; }

        Task<bool> MeridianFlip(Coordinates targetCoordinates, TimeSpan timeToFlip, CancellationToken cancellationToken = default);
    }
}