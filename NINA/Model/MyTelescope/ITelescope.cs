using ASCOM.DeviceInterface;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyTelescope {
    interface ITelescope : IDevice {
        string Name { get; }
        bool Connected { get; }
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
        bool Tracking { get; set; }
        double SiteLatitude { get; }
        double SiteLongitude { get; }
        double SiteElevation { get; }

        bool Connect();
        void Disconnect();
        bool MeridianFlip(Coordinates targetCoordinates);
        void MoveAxis(TelescopeAxes axis, double rate);
        void Park();
        void Setpark();
        void SlewToCoordinatesAsync(double ra, double dec);
        void SlewToCoordinates(double ra, double dec);
        void SlewToAltAz(double az, double alt);
        void SlewToAltAzAsync(double az, double alt);
        void StopSlew();
        bool Sync(string ra, string dec);
        bool Sync(double ra, double dec);
        void Unpark();
        void UpdateValues();
    }
}
