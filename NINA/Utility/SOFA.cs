#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.IO;
using System.Runtime.InteropServices;

namespace NINA.Utility {

    /// <summary>
    /// http://www.iausofa.org/current_C.html#Downloads
    /// </summary>
    internal static class SOFA {
        private const string DLLNAME = "SOFAlib.dll";

        static SOFA() {
            DllLoader.LoadDll(Path.Combine("SOFA", DLLNAME));
        }

        #region "Public Methods"

        /// <summary>
        /// Transform ICRS star data, epoch J2000.0, to CIRS.
        ///
        /// This function is part of the International Astronomical Union’s
        /// SOFA(Standards of Fundamental Astronomy) software collection.
        ///
        /// 1) Star data for an epoch other than J2000.0 (for example from the
        /// Hipparcos catalog, which has an epoch of J1991.25) will require a
        /// preliminary call to iauPmsafe before use.
        ///
        /// 2) The proper motion in RA is dRA/dt rather than cos(Dec)*dRA/dt.
        ///
        /// 3) The TDB date date1+date2 is a Julian Date, apportioned in any
        /// convenient way between the two arguments.For example,
        /// JD(TDB)=2450123.7 could be expressed in any of these ways, among
        /// others:
        ///
        /// date1 date2
        ///
        /// 2450123.7 0.0 (JD method)
        /// 2451545.0 −1421.3 (J2000 method)
        /// 2400000.5 50123.2 (MJD method)
        /// 2450123.5 0.2 (date & time method)
        ///
        /// The JD method is the most natural and convenient to use in cases
        /// where the loss of several decimal digits of resolution is
        /// acceptable.The J2000 method is best matched to the way the
        /// argument is handled internally and will deliver the optimum
        /// resolution.The MJD method and the date & time methods are both
        /// good compromises between resolution and convenience. For most
        /// applications of this function the choice will not be at all
        /// critical.
        ///
        /// TT can be used instead of TDB without any significant impact on
        /// accuracy.
        ///
        /// 4) The available accuracy is better than 1 milliarcsecond, limited
        /// mainly by the precession−nutation model that is used, namely
        /// IAU 2000A/2006. Very close to solar system bodies, additional
        /// errors of up to several milliarcseconds can occur because of
        /// unmodeled light deflection; however, the Sun’s contribution is
        /// taken into account, to first order.The accuracy limitations of
        /// the SOFA function iauEpv00(used to compute Earth position and
        /// velocity) can contribute aberration errors of up to
        /// 5 microarcseconds.Light deflection at the Sun’s limb is
        /// uncertain at the 0.4 mas level.
        ///
        /// 5) Should the transformation to(equinox based) apparent place be
        /// required rather than(CIO based) intermediate place, subtract the
        /// equation of the origins from the returned right ascension:
        /// RA = RI − EO. (The iauAnp function can then be applied, as
        /// required, to keep the result in the conventional 0−2pi range.)
        /// </summary>
        /// <param name="rc">ICRS right ascension at J2000.0 (radians, Note 1)</param>
        /// <param name="dc">ICRS declination at J2000.0 (radians, Note 1)</param>
        /// <param name="pr">RA proper motion (radians/year; Note 2)</param>
        /// <param name="pd">Dec proper motion (radians/year)</param>
        /// <param name="px">parallax (arcsec)</param>
        /// <param name="rv">radial velocity (km/s, +ve if receding)</param>
        /// <param name="date1">TDB as a 2−part...</param>
        /// <param name="date2">...Julian Date (Note 3)</param>
        /// <param name="ri">CIRS geocentric RA,Dec (radians)</param>
        /// <param name="di">CIRS geocentric RA,Dec (radians)</param>
        /// <param name="eo">equation of the origins (ERA−GST, Note 5)</param>
        public static void CelestialToIntermediate(double rc, double dc, double pr, double pd, double px, double rv, double date1, double date2, ref double ri, ref double di, ref double eo) {
            SOFA_Atci13(rc, dc, pr, pd, px, rv, date1, date2, ref ri, ref di, ref eo);
        }

        /// <summary>
        /// Transform star RA,Dec from geocentric CIRS to ICRS astrometric.
        ///
        /// This function is part of the International Astronomical Union’s
        /// SOFA(Standards of Fundamental Astronomy) software collection.
        ///
        /// Notes:
        ///
        /// 1) The TDB date date1+date2 is a Julian Date, apportioned in any
        /// convenient way between the two arguments.For example,
        /// JD(TDB)=2450123.7 could be expressed in any of these ways, among
        /// others:
        ///
        /// date1 date2
        ///
        /// 2450123.7 0.0 (JD method)
        /// 2451545.0 −1421.3 (J2000 method)
        /// 2400000.5 50123.2 (MJD method)
        /// 2450123.5 0.2 (date & time method)
        ///
        /// The JD method is the most natural and convenient to use in cases
        /// where the loss of several decimal digits of resolution is
        /// acceptable.The J2000 method is best matched to the way the
        /// argument is handled internally and will deliver the optimum
        /// resolution.The MJD method and the date & time methods are both
        /// good compromises between resolution and convenience. For most
        /// applications of this function the choice will not be at all
        /// critical.
        ///
        /// TT can be used instead of TDB without any significant impact on
        /// accuracy.
        ///
        /// 2) Iterative techniques are used for the aberration and light
        /// deflection corrections so that the functions iauAtic13(or
        /// iauAticq) and iauAtci13(or iauAtciq) are accurate inverses;
        /// even at the edge of the Sun’s disk the discrepancy is only about
        /// 1 nanoarcsecond.
        ///
        /// 3) The available accuracy is better than 1 milliarcsecond, limited
        /// mainly by the precession−nutation model that is used, namely
        /// IAU 2000A/2006. Very close to solar system bodies, additional
        /// errors of up to several milliarcseconds can occur because of
        /// unmodeled light deflection; however, the Sun’s contribution is
        /// taken into account, to first order.The accuracy limitations of
        /// the SOFA function iauEpv00(used to compute Earth position and
        /// velocity) can contribute aberration errors of up to
        /// 5 microarcseconds.Light deflection at the Sun’s limb is
        /// uncertain at the 0.4 mas level.
        ///
        /// 4) Should the transformation to(equinox based) J2000.0 mean place
        /// be required rather than(CIO based) ICRS coordinates, subtract the
        /// equation of the origins from the returned right ascension:
        /// RA = RI − EO. (The iauAnp function can then be applied, as
        /// required, to keep the result in the conventional 0−2pi range.)
        /// </summary>
        /// <param name="ri">CIRS geocentric RA (radians)</param>
        /// <param name="di">CIRS geocentric Dec (radians)</param>
        /// <param name="date1">TDB as a 2−part...</param>
        /// <param name="date2">...Julian Date (Note 1)</param>
        /// <param name="rc">ICRS astrometric RA(radians)</param>
        /// <param name="dc">ICRS astrometric Dec(radians)</param>
        /// <param name="eo">equation of the origins (ERA−GST, Note 4)</param>
        public static void IntermediateToCelestial(double ri, double di, double date1, double date2, ref double rc, ref double dc, ref double eo) {
            SOFA_Atic13(ri, di, date1, date2, ref rc, ref dc, ref eo);
        }

        /// <summary>
        /// Normalize angle into the range 0 &lt;= a &lt; 2pi.
        ///
        /// This function is part of the International Astronomical Union’s
        /// SOFA(Standards Of Fundamental Astronomy) software collection.
        /// </summary>
        /// <param name="a">angle (radians)</param>
        /// <returns>angle in range 0−2pi</returns>
        public static double Anp(double a) {
            return SOFA_Anp(a);
        }

        /// <summary>
        /// Equation of the origins, IAU 2006 precession and IAU 2000A nutation.
        ///
        /// Notes:
        ///
        /// 1) The TT date date1+date2 is a Julian Date, apportioned in any
        /// convenient way between the two arguments.For example,
        /// JD(TT)=2450123.7 could be expressed in any of these ways,
        /// among others:
        ///
        /// date1 date2
        ///
        /// 2450123.7 0.0 (JD method)
        /// 2451545.0 −1421.3 (J2000 method)
        /// 2400000.5 50123.2 (MJD method)
        /// 2450123.5 0.2 (date & time method)
        ///
        /// The JD method is the most natural and convenient to use in
        /// cases where the loss of several decimal digits of resolution
        /// is acceptable.The J2000 method is best matched to the way
        /// the argument is handled internally and will deliver the
        /// optimum resolution.The MJD method and the date & time methods
        /// are both good compromises between resolution and convenience.
        ///
        /// 2) The equation of the origins is the distance between the true
        /// equinox and the celestial intermediate origin and, equivalently,
        /// the difference between Earth rotation angle and Greenwich
        /// apparent sidereal time(ERA−GST). It comprises the precession
        /// (since J2000.0) in right ascension plus the equation of the
        /// equinoxes(including the small correction terms).
        /// </summary>
        /// <param name="date1">TT as a 2−part Julian Date (Note 1)</param>
        /// <param name="date2">TT as a 2−part Julian Date (Note 1)</param>
        /// <returns></returns>
        public static double Eo06a(double date1, double date2) {
            return SOFA_Eo06a(date1, date2);
        }

        /// <summary>
        /// Encode date and time fields into 2−part Julian Date (or in the case
        /// of UTC a quasi−JD form that includes special provision for leap
        /// seconds).
        ///
        /// Notes:
        ///
        /// 1) scale identifies the time scale.Only the value "UTC" (in upper
        /// case) is significant, and enables handling of leap seconds(see
        /// Note 4).
        ///
        /// 2) For calendar conventions and limitations, see iauCal2jd.
        ///
        /// 3) The sum of the results, d1+d2, is Julian Date, where normally d1
        /// is the Julian Day Number and d2 is the fraction of a day.In the
        /// case of UTC, where the use of JD is problematical, special
        /// conventions apply: see the next note.
        ///
        /// 4) JD cannot unambiguously represent UTC during a leap second unless
        /// special measures are taken.The SOFA internal convention is that
        /// the quasi−JD day represents UTC days whether the length is 86399,
        /// 86400 or 86401 SI seconds.In the 1960−1972 era there were
        /// smaller jumps (in either direction) each time the linear UTC(TAI)
        /// expression was changed, and these "mini−leaps" are also included
        /// in the SOFA convention.
        ///
        /// 5) The warning status "time is after end of day" usually means that
        /// the sec argument is greater than 60.0. However, in a day ending
        /// in a leap second the limit changes to 61.0 (or 59.0 in the case
        /// of a negative leap second).
        ///
        /// 6) The warning status "dubious year" flags UTCs that predate the
        /// introduction of the time scale or that are too far in the future
        /// to be trusted.See iauDat for further details.
        ///
        /// 7) Only in the case of continuous and regular time scales(TAI, TT,
        /// TCG, TCB and TDB) is the result d1+d2 a Julian Date, strictly
        /// speaking.In the other cases(UT1 and UTC) the result must be
        /// used with circumspection; in particular the difference between
        /// two such results cannot be interpreted as a precise time
        /// interval.
        /// </summary>
        /// <param name="scale">time scale ID (Note 1)</param>
        /// <param name="iy">year in Gregorian calendar (Note 2)</param>
        /// <param name="im">month in Gregorian calendar (Note 2)</param>
        /// <param name="id">day in Gregorian calendar (Note 2)</param>
        /// <param name="ihr">hour</param>
        /// <param name="imn">minute</param>
        /// <param name="sec">seconds</param>
        /// <param name="d1">2−part Julian Date (Notes 3,4)</param>
        /// <param name="d2">2−part Julian Date (Notes 3,4)</param>
        /// <returns></returns>
        public static short Dtf2d(string scale, int iy, int im, int id, int ihr, int imn, double sec, ref double d1, ref double d2) {
            return SOFA_Dtf2d(scale, iy, im, id, ihr, imn, sec, ref d1, ref d2);
        }

        /// <summary>
        /// Time scale transformation: Coordinated Universal Time, UTC, to
        /// International Atomic Time, TAI.
        ///
        /// Notes:
        ///
        /// 1) utc1+utc2 is quasi Julian Date(see Note 2), apportioned in any
        /// convenient way between the two arguments, for example where utc1
        /// is the Julian Day Number and utc2 is the fraction of a day.
        ///
        /// 2) JD cannot unambiguously represent UTC during a leap second unless
        /// special measures are taken.The convention in the present
        /// function is that the JD day represents UTC days whether the
        /// length is 86399, 86400 or 86401 SI seconds. In the 1960−1972 era
        /// there were smaller jumps (in either direction) each time the
        /// linear UTC(TAI) expression was changed, and these "mini−leaps"
        /// are also included in the SOFA convention.
        ///
        /// 3) The warning status "dubious year" flags UTCs that predate the
        /// introduction of the time scale or that are too far in the future
        /// to be trusted.See iauDat for further details.
        ///
        /// 4) The function iauDtf2d converts from calendar date and time of day
        /// into 2−part Julian Date, and in the case of UTC implements the
        /// leap−second−ambiguity convention described above.
        ///
        /// 5) The returned TAI1,TAI2 are such that their sum is the TAI Julian
        /// Date.
        /// </summary>
        /// <param name="utc1">UTC as a 2−part quasi Julian Date (Notes 1−4)</param>
        /// <param name="utc2">UTC as a 2−part quasi Julian Date (Notes 1−4)</param>
        /// <param name="tai1">TAI as a 2−part Julian Date (Note 5)</param>
        /// <param name="tai2">TAI as a 2−part Julian Date (Note 5)</param>
        /// <returns>int status: +1 = dubious year (Note 3); 0 = OK; −1 = unacceptable date</returns>
        public static short UtcTai(double utc1, double utc2, ref double tai1, ref double tai2) {
            return SOFA_Utctai(utc1, utc2, ref tai1, ref tai2);
        }

        /// <summary>
        /// Time scale transformation: International Atomic Time, TAI, to
        /// Terrestrial Time, TT.
        ///
        /// Note:
        ///
        /// tai1+tai2 is Julian Date, apportioned in any convenient way
        /// between the two arguments, for example where tai1 is the Julian
        /// Day Number and tai2 is the fraction of a day.The returned
        /// tt1,tt2 follow suit.
        /// </summary>
        /// <param name="tai1">TAI as a 2−part Julian Date</param>
        /// <param name="tai2">TAI as a 2−part Julian Date</param>
        /// <param name="tt1">TT as a 2−part Julian Date</param>
        /// <param name="tt2">TT as a 2−part Julian Date</param>
        /// <returns>status 0 = ok</returns>
        public static short TaiTt(double tai1, double tai2, ref double tt1, ref double tt2) {
            return SOFA_Taitt(tai1, tai2, ref tt1, ref tt2);
        }

        /// <summary>
        /// Horizon to equatorial coordinates: transform azimuth and altitude
        /// to hour angle and declination.
        ///
        /// Given:
        /// az double azimuth
        /// el double altitude (informally, elevation)
        /// phi double site latitude
        ///
        /// Returned:
        /// ha double hour angle (local)
        /// dec double declination
        ///
        /// Notes:
        ///
        /// 1) All the arguments are angles in radians.
        ///
        /// 2) The sign convention for azimuth is north zero, east +pi/2.
        ///
        /// 3) HA is returned in the range +/−pi. Declination is returned in
        /// the range +/−pi/2.
        ///
        /// 4) The latitude phi is pi/2 minus the angle between the Earth’s
        /// rotation axis and the adopted zenith. In many applications it
        /// will be sufficient to use the published geodetic latitude of the
        /// site. In very precise (sub−arcsecond) applications, phi can be
        /// corrected for polar motion.
        ///
        /// 5) The azimuth az must be with respect to the rotational north pole,
        /// as opposed to the ITRS pole, and an azimuth with respect to north
        /// on a map of the Earth’s surface will need to be adjusted for
        /// polar motion if sub−arcsecond accuracy is required.
        ///
        /// 6) Should the user wish to work with respect to the astronomical
        /// zenith rather than the geodetic zenith, phi will need to be
        /// adjusted for deflection of the vertical (often tens of
        /// arcseconds), and the zero point of ha will also be affected.
        ///
        /// 7) The transformation is the same as Ve = Ry(phi−pi/2)*Rz(pi)*Vh,
        /// where Ve and Vh are lefthanded unit vectors in the (ha,dec) and
        /// (az,el) systems respectively and Rz and Ry are rotations about
        /// first the z−axis and then the y−axis. (n.b. Rz(pi) simply
        /// reverses the signs of the x and y components.) For efficiency,
        /// the algorithm is written out rather than calling other utility
        /// functions. For applications that require even greater
        /// efficiency, additional savings are possible if constant terms
        /// such as functions of latitude are computed once and for all.
        ///
        /// 8) Again for efficiency, no range checking of arguments is carried
        /// out.
        /// </summary>
        /// <param name="azimuth">azimuth</param>
        /// <param name="altitude">altitude</param>
        /// <param name="latitude">site latitude</param>
        /// <param name="hourAngle">hour angle (local)</param>
        /// <param name="declination">declination</param>
        /// <returns></returns>
        public static short Ae2hd(double azimuth, double altitude, double latitude, ref double hourAngle, ref double declination) {
            return SOFA_Ae2hd(azimuth, altitude, latitude, ref hourAngle, ref declination);
        }

        /// <summary>
        /// Equatorial to horizon coordinates: transform hour angle and
        /// declination to azimuth and altitude.
        ///
        /// This function is part of the International Astronomical Union’s
        /// SOFA (Standards of Fundamental Astronomy) software collection.
        ///
        /// Status: support function.
        ///
        /// Given:
        /// ha double hour angle (local)
        /// dec double declination
        /// phi double site latitude
        ///
        /// Returned:
        /// *az double azimuth
        /// *el double altitude (informally, elevation)
        ///
        /// Notes:
        ///
        /// 1) All the arguments are angles in radians.
        ///
        /// 2) Azimuth is returned in the range 0−2pi; north is zero, and east
        /// is +pi/2. Altitude is returned in the range +/− pi/2.
        ///
        /// 3) The latitude phi is pi/2 minus the angle between the Earth’s
        /// rotation axis and the adopted zenith. In many applications it
        /// will be sufficient to use the published geodetic latitude of the
        /// site. In very precise (sub−arcsecond) applications, phi can be
        /// corrected for polar motion.
        ///
        /// 4) The returned azimuth az is with respect to the rotational north
        /// pole, as opposed to the ITRS pole, and for sub−arcsecond
        /// accuracy will need to be adjusted for polar motion if it is to
        /// be with respect to north on a map of the Earth’s surface.
        ///
        /// 5) Should the user wish to work with respect to the astronomical
        /// zenith rather than the geodetic zenith, phi will need to be
        /// adjusted for deflection of the vertical (often tens of
        /// arcseconds), and the zero point of the hour angle ha will also
        /// be affected.
        ///
        /// 6) The transformation is the same as Vh = Rz(pi)*Ry(pi/2−phi)*Ve,
        /// where Vh and Ve are lefthanded unit vectors in the (az,el) and
        /// (ha,dec) systems respectively and Ry and Rz are rotations about
        /// first the y−axis and then the z−axis. (n.b. Rz(pi) simply
        /// reverses the signs of the x and y components.) For efficiency,
        /// the algorithm is written out rather than calling other utility
        /// functions. For applications that require even greater
        /// efficiency, additional savings are possible if constant terms
        /// such as functions of latitude are computed once and for all.
        ///
        /// 7) Again for efficiency, no range checking of arguments is carried
        /// out.
        /// </summary>
        /// <param name="hourAngle">hour angle (local)</param>
        /// <param name="declination">declination</param>
        /// <param name="latitude">site latitude</param>
        /// <param name="azimuth">azimuth</param>
        /// <param name="altitude">altitude</param>
        /// <returns></returns>
        public static short Hd2ae(double hourAngle, double declination, double latitude, ref double azimuth, ref double altitude) {
            return SOFA_Hd2ae(hourAngle, declination, latitude, ref azimuth, ref altitude);
        }

        /// <summary>
        /// ICRS RA,Dec to observed place. The caller supplies UTC, site
        /// coordinates, ambient air conditions and observing wavelength.
        ///
        /// SOFA models are used for the Earth ephemeris, bias−precession−
        /// nutation, Earth orientation and refraction.
        ///
        /// This function is part of the International Astronomical Union’s
        /// SOFA (Standards of Fundamental Astronomy) software collection.
        ///
        /// Status: support function.
        ///
        /// Given:
        /// rc,dc double ICRS right ascension at J2000.0 (radians, Note 1)
        /// pr double RA proper motion (radians/year; Note 2)
        /// pd double Dec proper motion (radians/year)
        /// px double parallax (arcsec)
        /// rv double radial velocity (km/s, +ve if receding)
        /// utc1 double UTC as a 2−part...
        /// utc2 double ...quasi Julian Date (Notes 3−4)
        /// dut1 double UT1−UTC (seconds, Note 5)
        /// elong double longitude (radians, east +ve, Note 6)
        /// phi double latitude (geodetic, radians, Note 6)
        /// hm double height above ellipsoid (m, geodetic, Notes 6,8)
        /// xp,yp double polar motion coordinates (radians, Note 7)
        /// phpa double pressure at the observer (hPa = mB, Note 8)
        /// tc double ambient temperature at the observer (deg C)
        /// rh double relative humidity at the observer (range 0−1)
        /// wl double wavelength (micrometers, Note 9)
        ///
        /// Returned:
        /// aob double* observed azimuth (radians: N=0,E=90)
        /// zob double* observed zenith distance (radians)
        /// hob double* observed hour angle (radians)
        /// dob double* observed declination (radians)
        /// rob double* observed right ascension (CIO−based, radians)
        /// eo double* equation of the origins (ERA−GST)
        ///
        /// Returned (function value):
        /// int status: +1 = dubious year (Note 4)
        /// 0 = OK
        /// −1 = unacceptable date
        ///
        /// Notes:
        ///
        /// 1) Star data for an epoch other than J2000.0 (for example from the
        /// Hipparcos catalog, which has an epoch of J1991.25) will require
        /// a preliminary call to iauPmsafe before use.
        ///
        /// 2) The proper motion in RA is dRA/dt rather than cos(Dec)*dRA/dt.
        ///
        /// 3) utc1+utc2 is quasi Julian Date (see Note 2), apportioned in any
        /// convenient way between the two arguments, for example where utc1
        /// is the Julian Day Number and utc2 is the fraction of a day.
        ///
        /// However, JD cannot unambiguously represent UTC during a leap
        /// second unless special measures are taken. The convention in the
        /// present function is that the JD day represents UTC days whether
        /// the length is 86399, 86400 or 86401 SI seconds.
        ///
        /// Applications should use the function iauDtf2d to convert from
        /// calendar date and time of day into 2−part quasi Julian Date, as
        /// it implements the leap−second−ambiguity convention just
        /// described.
        ///
        /// 4) The warning status "dubious year" flags UTCs that predate the
        /// introduction of the time scale or that are too far in the
        /// future to be trusted. See iauDat for further details.
        ///
        /// 5) UT1−UTC is tabulated in IERS bulletins. It increases by exactly
        /// one second at the end of each positive UTC leap second,
        /// introduced in order to keep UT1−UTC within +/− 0.9s. n.b. This
        /// practice is under review, and in the future UT1−UTC may grow
        /// essentially without limit.
        ///
        /// 6) The geographical coordinates are with respect to the WGS84
        /// reference ellipsoid. TAKE CARE WITH THE LONGITUDE SIGN: the
        /// longitude required by the present function is east−positive
        /// (i.e. right−handed), in accordance with geographical convention.
        ///
        /// 7) The polar motion xp,yp can be obtained from IERS bulletins. The
        /// values are the coordinates (in radians) of the Celestial
        /// Intermediate Pole with respect to the International Terrestrial
        /// Reference System (see IERS Conventions 2003), measured along the
        /// meridians 0 and 90 deg west respectively. For many
        /// applications, xp and yp can be set to zero.
        ///
        /// 8) If hm, the height above the ellipsoid of the observing station
        /// in meters, is not known but phpa, the pressure in hPa (=mB),
        /// is available, an adequate estimate of hm can be obtained from
        /// the expression
        ///
        /// hm = −29.3 * tsl * log ( phpa / 1013.25 );
        ///
        /// where tsl is the approximate sea−level air temperature in K
        /// (See Astrophysical Quantities, C.W.Allen, 3rd edition, section
        /// 52). Similarly, if the pressure phpa is not known, it can be
        /// estimated from the height of the observing station, hm, as
        /// follows:
        ///
        /// phpa = 1013.25 * exp ( −hm / ( 29.3 * tsl ) );
        ///
        /// Note, however, that the refraction is nearly proportional to
        /// the pressure and that an accurate phpa value is important for
        /// precise work.
        ///
        /// 9) The argument wl specifies the observing wavelength in
        /// micrometers. The transition from optical to radio is assumed to
        /// occur at 100 micrometers (about 3000 GHz).
        ///
        /// 10) The accuracy of the result is limited by the corrections for
        /// refraction, which use a simple A*tan(z) + B*tan^3(z) model.
        /// Providing the meteorological parameters are known accurately and
        /// there are no gross local effects, the predicted observed
        /// coordinates should be within 0.05 arcsec (optical) or 1 arcsec
        /// (radio) for a zenith distance of less than 70 degrees, better
        /// than 30 arcsec (optical or radio) at 85 degrees and better
        /// than 20 arcmin (optical) or 30 arcmin (radio) at the horizon.
        ///
        /// Without refraction, the complementary functions iauAtco13 and
        /// iauAtoc13 are self−consistent to better than 1 microarcsecond
        /// all over the celestial sphere. With refraction included,
        /// consistency falls off at high zenith distances, but is still
        /// better than 0.05 arcsec at 85 degrees.
        ///
        /// 11) "Observed" Az,ZD means the position that would be seen by a
        /// perfect geodetically aligned theodolite. (Zenith distance is
        /// used rather than altitude in order to reflect the fact that no
        /// allowance is made for depression of the horizon.) This is
        /// related to the observed HA,Dec via the standard rotation, using
        /// the geodetic latitude (corrected for polar motion), while the
        /// observed HA and RA are related simply through the Earth rotation
        /// angle and the site longitude. "Observed" RA,Dec or HA,Dec thus
        /// means the position that would be seen by a perfect equatorial
        /// with its polar axis aligned to the Earth’s axis of rotation.
        ///
        /// 12) It is advisable to take great care with units, as even unlikely
        /// values of the input parameters are accepted and processed in
        /// accordance with the models used.
        /// </summary>
        /// <param name="rc">right ascension (J2000) in radians</param>
        /// <param name="dc">declination (J2000) in radians</param>
        /// <param name="pr">right ascension proper motion (radians/year; Note 2)</param>
        /// <param name="pd">declination proper motion (radians/year)</param>
        /// <param name="px">parallax (arcsec)</param>
        /// <param name="rv">radial velocity (km/s, +ve if receding)</param>
        /// <param name="utc1">UTC as a 2−part quasi Julian Date (Notes 3−4)</param>
        /// <param name="utc2">UTC as a 2−part quasi Julian Date (Notes 3−4)</param>
        /// <param name="dut1">UT1−UTC (seconds, Note 5)</param>
        /// <param name="elong">longitude (radians, east +ve, Note 6)</param>
        /// <param name="phi">latitude (geodetic, radians, Note 6)</param>
        /// <param name="hm">height above ellipsoid (m, geodetic, Notes 6, 8)</param>
        /// <param name="xp">polar motion coordinate x (radians, note 7)</param>
        /// <param name="yp">polar motion coordinate y (radians, note 7)</param>
        /// <param name="phpa">pressure at the observer (hPa = mB, Note 8)</param>
        /// <param name="tc">ambient temperature at the observer (deg C)</param>
        /// <param name="rh">relative humidity at the observer (deg C)</param>
        /// <param name="wl">wavelength (micrometers, Note 9)</param>
        /// <param name="aob">retunred observed azimuth (radians: N=0,E=90)</param>
        /// <param name="zob">retunred observed zenith distance</param>
        /// <param name="hob">returned observed hour angle (radians)</param>
        /// <param name="dob">returned observed declination (radians)</param>
        /// <param name="rob">returned observed right ascension (CIO−based, radians)</param>
        /// <param name="eo">returned equation of the origins (ERA−GST)</param>
        /// <returns></returns>
        public static short CelestialToTopocentric(double rc, double dc, double pr, double pd, double px, double rv, double utc1, double utc2, double dut1, double elong, double phi, double hm, double xp, double yp, double phpa, double tc, double rh, double wl, ref double aob, ref double zob, ref double hob, ref double dob, ref double rob, ref double eo) {
            return SOFA_Atco13(rc, dc, pr, pd, px, rv, utc1, utc2, dut1, elong, phi, hm, xp, yp, phpa, tc, rh, wl, ref aob, ref zob, ref hob, ref dob, ref rob, ref eo);
        }

        /// <summary>
        /// Observed place at a groundbased site to to ICRS astrometric RA,Dec.
        /// The caller supplies UTC, site coordinates, ambient air conditions
        /// and observing wavelength.
        ///
        /// This function is part of the International Astronomical Union’s
        /// SOFA (Standards of Fundamental Astronomy) software collection.
        ///
        /// Status: support function.
        ///
        /// Given:
        /// type char[] type of coordinates − "R", "H" or "A" (Notes 1,2)
        /// ob1 double observed Az, HA or RA (radians; Az is N=0,E=90)
        /// ob2 double observed ZD or Dec (radians)
        /// utc1 double UTC as a 2−part...
        /// utc2 double ...quasi Julian Date (Notes 3,4)
        /// dut1 double UT1−UTC (seconds, Note 5)
        /// elong double longitude (radians, east +ve, Note 6)
        /// phi double geodetic latitude (radians, Note 6)
        /// hm double height above ellipsoid (m, geodetic Notes 6,8)
        /// xp,yp double polar motion coordinates (radians, Note 7)
        /// phpa double pressure at the observer (hPa = mB, Note 8)
        /// tc double ambient temperature at the observer (deg C)
        /// rh double relative humidity at the observer (range 0−1)
        /// wl double wavelength (micrometers, Note 9)
        ///
        /// Returned:
        /// rc,dc double ICRS astrometric RA,Dec (radians)
        ///
        /// Returned (function value):
        /// int status: +1 = dubious year (Note 4)
        /// 0 = OK
        /// −1 = unacceptable date
        ///
        /// Notes:
        ///
        /// 1) "Observed" Az,ZD means the position that would be seen by a
        /// perfect geodetically aligned theodolite. (Zenith distance is
        /// used rather than altitude in order to reflect the fact that no
        /// allowance is made for depression of the horizon.) This is
        /// related to the observed HA,Dec via the standard rotation, using
        /// the geodetic latitude (corrected for polar motion), while the
        /// observed HA and RA are related simply through the Earth rotation
        /// angle and the site longitude. "Observed" RA,Dec or HA,Dec thus
        /// means the position that would be seen by a perfect equatorial
        /// with its polar axis aligned to the Earth’s axis of rotation.
        ///
        /// 2) Only the first character of the type argument is significant.
        /// "R" or "r" indicates that ob1 and ob2 are the observed right
        /// ascension and declination; "H" or "h" indicates that they are
        /// hour angle (west +ve) and declination; anything else ("A" or
        /// "a" is recommended) indicates that ob1 and ob2 are azimuth
        /// (north zero, east 90 deg) and zenith distance.
        ///
        /// 3) utc1+utc2 is quasi Julian Date (see Note 2), apportioned in any
        /// convenient way between the two arguments, for example where utc1
        /// is the Julian Day Number and utc2 is the fraction of a day.
        ///
        /// However, JD cannot unambiguously represent UTC during a leap
        /// second unless special measures are taken. The convention in the
        /// present function is that the JD day represents UTC days whether
        /// the length is 86399, 86400 or 86401 SI seconds.
        ///
        /// Applications should use the function iauDtf2d to convert from
        /// calendar date and time of day into 2−part quasi Julian Date, as
        /// it implements the leap−second−ambiguity convention just
        /// described.
        ///
        /// 4) The warning status "dubious year" flags UTCs that predate the
        /// introduction of the time scale or that are too far in the
        /// future to be trusted. See iauDat for further details.
        ///
        /// 5) UT1−UTC is tabulated in IERS bulletins. It increases by exactly
        /// one second at the end of each positive UTC leap second,
        /// introduced in order to keep UT1−UTC within +/− 0.9s. n.b. This
        /// practice is under review, and in the future UT1−UTC may grow
        /// essentially without limit.
        ///
        /// 6) The geographical coordinates are with respect to the WGS84
        /// reference ellipsoid. TAKE CARE WITH THE LONGITUDE SIGN: the
        /// longitude required by the present function is east−positive
        /// (i.e. right−handed), in accordance with geographical convention.
        ///
        /// 7) The polar motion xp,yp can be obtained from IERS bulletins. The
        /// values are the coordinates (in radians) of the Celestial
        /// Intermediate Pole with respect to the International Terrestrial
        /// Reference System (see IERS Conventions 2003), measured along the
        /// meridians 0 and 90 deg west respectively. For many
        /// applications, xp and yp can be set to zero.
        ///
        /// 8) If hm, the height above the ellipsoid of the observing station
        /// in meters, is not known but phpa, the pressure in hPa (=mB), is
        /// available, an adequate estimate of hm can be obtained from the
        /// expression
        ///
        /// hm = −29.3 * tsl * log ( phpa / 1013.25 );
        ///
        /// where tsl is the approximate sea−level air temperature in K
        /// (See Astrophysical Quantities, C.W.Allen, 3rd edition, section
        /// 52). Similarly, if the pressure phpa is not known, it can be
        /// estimated from the height of the observing station, hm, as
        /// follows:
        ///
        /// phpa = 1013.25 * exp ( −hm / ( 29.3 * tsl ) );
        ///
        /// Note, however, that the refraction is nearly proportional to
        /// the pressure and that an accurate phpa value is important for
        /// precise work.
        ///
        /// 9) The argument wl specifies the observing wavelength in
        /// micrometers. The transition from optical to radio is assumed to
        /// occur at 100 micrometers (about 3000 GHz).
        ///
        /// 10) The accuracy of the result is limited by the corrections for
        /// refraction, which use a simple A*tan(z) + B*tan^3(z) model.
        /// Providing the meteorological parameters are known accurately and
        /// there are no gross local effects, the predicted astrometric
        /// coordinates should be within 0.05 arcsec (optical) or 1 arcsec
        /// (radio) for a zenith distance of less than 70 degrees, better
        /// than 30 arcsec (optical or radio) at 85 degrees and better
        /// than 20 arcmin (optical) or 30 arcmin (radio) at the horizon.
        ///
        /// Without refraction, the complementary functions iauAtco13 and
        /// iauAtoc13 are self−consistent to better than 1 microarcsecond
        /// all over the celestial sphere. With refraction included,
        /// consistency falls off at high zenith distances, but is still
        /// better than 0.05 arcsec at 85 degrees.
        ///
        /// 11) It is advisable to take great care with units, as even unlikely
        /// values of the input parameters are accepted and processed in
        /// accordance with the models used.
        /// </summary>
        /// <param name="type">type of coordinates − "R", "H" or "A" (Notes 1,2)</param>
        /// <param name="ob1">observed Az, HA or RA (radians; Az is N=0,E=90)</param>
        /// <param name="ob2">observed ZD or Dec (radians)</param>
        /// <param name="utc1">UTC as a 2−part quasi Julian Date (Notes 3−4)</param>
        /// <param name="utc2">UTC as a 2−part quasi Julian Date (Notes 3−4)</param>
        /// <param name="dut1">UT1−UTC (seconds, Note 5)</param>
        /// <param name="elong">longitude (radians, east +ve, Note 6)</param>
        /// <param name="phi">latitude (radians, Note 6)</param>
        /// <param name="hm">height above ellipsoid (m, geodetic Notes 6,8)</param>
        /// <param name="xp">polar motion coordinates (radians, Note 7)</param>
        /// <param name="yp">polar motion coordinates (radians, Note 7)</param>
        /// <param name="phpa">pressure at the observer (hPa = mB, Note 8)</param>
        /// <param name="tc">ambient temperature at the observer (deg C)</param>
        /// <param name="rh">relative humidity at the observer (range 0−1)</param>
        /// <param name="wl">wavelength (micrometers, Note 9)</param>
        /// <param name="rc">ICRS astrometric RA (radians)</param>
        /// <param name="dc">ICRS astrometric Dec (radians)</param>
        /// <returns></returns>
        public static short TopocentricToCelestial(string type, double ob1, double ob2, double utc1, double utc2, double dut1, double elong, double phi, double hm, double xp, double yp, double phpa, double tc, double rh, double wl, ref double rc, ref double dc) {
            return SOFA_Atoc13(type, ob1, ob2, utc1, utc2, dut1, elong, phi, hm, xp, yp, phpa, tc, rh, wl, ref rc, ref dc);
        }

        #endregion "Public Methods"

        #region "External DLL calls"

        [DllImport(DLLNAME, EntryPoint = "iauAtci13", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SOFA_Atci13(double rc, double dc, double pr, double pd, double px, double rv, double date1, double date2, ref double ri, ref double di, ref double eo);

        [DllImport(DLLNAME, EntryPoint = "iauAtic13", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SOFA_Atic13(double ri, double di, double date1, double date2, ref double rc, ref double dc, ref double eo);

        [DllImport(DLLNAME, EntryPoint = "iauAnp", CallingConvention = CallingConvention.Cdecl)]
        private static extern double SOFA_Anp(double a);

        [DllImport(DLLNAME, EntryPoint = "iauEo06a", CallingConvention = CallingConvention.Cdecl)]
        private static extern double SOFA_Eo06a(double date1, double date2);

        [DllImport(DLLNAME, EntryPoint = "iauDtf2d", CallingConvention = CallingConvention.Cdecl)]
        private static extern short SOFA_Dtf2d(string scale, int iy, int im, int id, int ihr, int imn, double sec, ref double d1, ref double d2);

        [DllImport(DLLNAME, EntryPoint = "iauUtctai", CallingConvention = CallingConvention.Cdecl)]
        private static extern short SOFA_Utctai(double utc1, double utc2, ref double tai1, ref double tai2);

        [DllImport(DLLNAME, EntryPoint = "iauTaitt", CallingConvention = CallingConvention.Cdecl)]
        private static extern short SOFA_Taitt(double tai1, double tai2, ref double tt1, ref double tt2);

        [DllImport(DLLNAME, EntryPoint = "iauAe2hd", CallingConvention = CallingConvention.Cdecl)]
        private static extern short SOFA_Ae2hd(double az, double el, double phi, ref double ha, ref double dec);

        [DllImport(DLLNAME, EntryPoint = "iauHd2ae", CallingConvention = CallingConvention.Cdecl)]
        private static extern short SOFA_Hd2ae(double ha, double dec, double phi, ref double az, ref double el);

        [DllImport(DLLNAME, EntryPoint = "iauAtco13", CallingConvention = CallingConvention.Cdecl)]
        private static extern short SOFA_Atco13(double rc, double dc, double pr, double pd, double px, double rv, double utc1, double utc2, double dut1, double elong, double phi, double hm, double xp, double yp, double phpa, double tc, double rh, double wl, ref double aob, ref double zob, ref double hob, ref double dob, ref double rob, ref double eo);

        [DllImport(DLLNAME, EntryPoint = "iauAtoc13", CallingConvention = CallingConvention.Cdecl)]
        private static extern short SOFA_Atoc13(string type, double ob1, double ob2, double utc1, double utc2, double dut1, double elong, double phi, double hm, double xp, double yp, double phpa, double tc, double rh, double wl, ref double rc, ref double dc);

        #endregion "External DLL calls"
    }
}
