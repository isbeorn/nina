using ASCOM.DeviceInterface;
using NINA.Utility.Astrometry;

namespace NINA.Model.MyTelescope {

    internal interface ITelescope : IDevice {
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
        double SiteLatitude { get; set; }
        double SiteLongitude { get; set; }
        double SiteElevation { get; }
        bool CanSetSiteLatLong { get; }

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

        void SendCommandString(string command);
    }
}