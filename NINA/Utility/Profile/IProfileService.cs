using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Profile {

    public interface IProfileService {
        Profiles Profiles { get; }
        IProfile ActiveProfile { get; }

        void Clone(Guid guid);

        void Add();

        void SelectProfile(Guid guid);

        void RemoveProfile(Guid guid);

        void ChangeLocale(CultureInfo language);

        void ChangeHemisphere(Hemisphere hemisphere);

        void ChangeLatitude(double latitude);

        void ChangeLongitude(double longitude);

        event EventHandler LocaleChanged;

        event EventHandler LocationChanged;

        event EventHandler ProfileChanged;
    }
}