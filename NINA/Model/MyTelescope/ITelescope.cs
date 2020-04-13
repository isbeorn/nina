#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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