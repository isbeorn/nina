#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

        #endregion "External DLL calls"
    }
}