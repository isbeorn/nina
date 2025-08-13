#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Interfaces {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ShutterState {

        [Description("LblUnknown")]
        ShutterNone = -1,

        [Description("LblOpen")]
        ShutterOpen = 0,

        [Description("LblClosed")]
        ShutterClosed = 1,

        [Description("LblOpening")]
        ShutterOpening = 2,

        [Description("LblClosing")]
        ShutterClosing = 3,

        [Description("LblError")]
        ShutterError = 4
    }

    public interface IDome : IDevice {
        ShutterState ShutterStatus { get; }
        bool DriverCanFollow { get; }
        bool CanSetShutter { get; }
        bool CanSetPark { get; }
        bool CanSetAzimuth { get; }
        bool CanSyncAzimuth { get; }
        bool CanPark { get; }
        bool CanFindHome { get; }
        double Azimuth { get; }
        double Altitude { get; }
        bool AtPark { get; }
        bool AtHome { get; }
        bool DriverFollowing { get; set; }
        bool Slewing { get; }

        Task SlewToAzimuth(double azimuth, CancellationToken ct);

        Task StopSlewing();

        Task StopShutter();

        Task StopAll();

        Task OpenShutter(CancellationToken ct);

        Task CloseShutter(CancellationToken ct);

        Task FindHome(CancellationToken ct);

        Task Park(CancellationToken ct);

        void SetPark();

        void SyncToAzimuth(double azimuth);
    }
}