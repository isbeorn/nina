#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using System.Threading.Tasks;

namespace NINA.Model.MyTelescope {

    internal interface ITelescope : IDevice {
        Coordinates Coordinates { get; }
        double RightAscension { get; }
        string RightAscensionString { get; }
        double Declination { get; }
        string DeclinationString { get; }
        double SiderealTime { get; }
        string SiderealTimeString { get; }
        double Altitude { get; }
        string AltitudeString { get; }
        double Azimuth { get; }
        string AzimuthString { get; }
        double HoursToMeridian { get; }
        string HoursToMeridianString { get; }
        double TimeToMeridianFlip { get; }
        string TimeToMeridianFlipString { get; }
        double MovingRate { get; set; }
        PierSide SideOfPier { get; }
        bool CanSetTracking { get; }
        bool Tracking { get; set; }
        double SiteLatitude { get; set; }
        double SiteLongitude { get; set; }
        double SiteElevation { get; }
        bool CanSetSiteLatLong { get; }
        bool AtPark { get; }
        bool CanPark { get; }
        bool CanUnpark { get; }
        bool CanSetPark { get; }
        Epoch EquatorialSystem { get; }
        bool HasUnknownEpoch { get; }

        Task<bool> MeridianFlip(Coordinates targetCoordinates);

        void MoveAxis(TelescopeAxes axis, double rate);

        void PulseGuide(GuideDirections direction, int duration);

        void Park();

        void Setpark();

        void SlewToCoordinatesAsync(Coordinates coordinates);

        void SlewToCoordinates(Coordinates coordinates);

        void SlewToAltAz(TopocentricCoordinates coordinates);

        void SlewToAltAzAsync(TopocentricCoordinates coordinates);

        void StopSlew();

        bool Sync(Coordinates coordinates);

        void Unpark();

        void SendCommandString(string command);
    }
}
