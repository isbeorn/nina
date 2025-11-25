#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.INDI.Enums;

namespace NINA.INDI.Interfaces {
    public interface IINDITelescope : IINDIDevice {
        AlignmentMode AlignmentMode { get; }
        double Altitude { get; }
        double ApertureArea { get; }
        double ApertureDiameter { get; }
        bool AtHome { get; }
        bool AtPark { get; }
        double Azimuth { get; }
        double Declination { get; }
        double DeclinationRate { get; set; }
        bool DoesRefraction { get; }
        double FocalLength { get; }
        double GuideRateDeclination { get; }
        double GuideRateRightAscension { get; }
        bool IsPulseGuiding { get; }
        double RightAscension { get; }
        double RightAscensionRate { get; set; }
        PierSide SideOfPier { get; }
        double SiderealTime { get; }
        double SiteElevation { get; }
        double SiteLatitude { get; }
        double SiteLongitude { get; }
        int SlewSettleTime { get; }
        bool Slewing { get; }
        double TargetDeclination { get; }
        double TargetRightAscension { get; }
        bool Tracking { get; set; }





        DateTime UTCDate { get; set; }


        void AbortSlew();
        IAxisRates AxisRates(TelescopeAxes axis);
        void ConfigureJNOW();
        void MoveAxis(TelescopeAxes axis, double rate);
        void PulseGuide(GuideDirections direction, int duration);
        Task ParkAsync(CancellationToken ct = default);
        void SetPark();
        void SetTrackingMode(TrackingMode mode);
        TrackingMode GetTrackingMode();
        IList<TrackingMode> GetSupportedTrackingModes();
        void SlewToCoordinates(double ra, double dec);
        Task SlewToCoordinatesTaskAsync(double ra, double dec, CancellationToken ct = default);
        void SlewToAltAz(double Azimuth, double Altitude);
        Task SlewToAltAzTaskAsync(double azimuth, double altitude, CancellationToken cancellationToken = default);
        void SyncToCoordinates(double ra, double dec);
        Task FindHomeAsync(CancellationToken ct = default);
        Task UnparkAsync(CancellationToken ct = default);
        bool CanMoveAxis(TelescopeAxes axis);
        PierSide DestinationSideOfPier(double ra, double dec);
    }
}
