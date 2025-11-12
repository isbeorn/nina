#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.ViewModel;
using OxyPlot;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Interfaces.Mediator {

    public interface IFocuserMediator : IDeviceMediator<IFocuserVM, IFocuserConsumer, FocuserInfo> {

        void ToggleTempComp(bool tempComp);

        Task<int> MoveFocuser(int position, CancellationToken ct);

        Task<int> MoveFocuserRelative(int position, CancellationToken ct);

        Task<int> MoveFocuserByTemperatureRelative(double temperature, double Slope, CancellationToken ct);

        void BroadcastSuccessfulAutoFocusRun(AutoFocusInfo info);
        void BroadcastNewAutoFocusPoint(DataPoint dataPoint);

        void BroadcastUserFocused(FocuserInfo info);
        void BroadcastAutoFocusRunStarting();
    }
}