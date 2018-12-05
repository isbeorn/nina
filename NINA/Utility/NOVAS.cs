using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {

    internal static class NOVAS {
        private const string DLLNAME = "NOVAS31.dll";

        static NOVAS() {
            DllLoader.LoadDll("NOVAS/" + DLLNAME);
        }

        [DllImport(DLLNAME, EntryPoint = "julian_date")]
        private static extern double NOVAS_JulianDate(short year, short month, short day, double hour);

        [DllImport(DLLNAME, EntryPoint = "sidereal_time")]
        private static extern short NOVAS_SiderealTime(double jdHigh, double jdLow, double detlaT, GstType gstType, Method method, Accuracy accuracy, ref double gst);

        public static double JulianDate(short year, short month, short day, double hour) {
            return NOVAS_JulianDate(year, month, day, hour);
        }

        public enum GstType : short {
            GreenwichMeanSiderealTime = 0,
            GreenwichApparentSiderealTime = 1
        }

        public enum Method : short {
            CIOBased = 0,
            EquinoxBased = 1
        }

        public enum Accuracy : short {
            Full = 0,
            Reduced = 1
        }

        public static short SiderealTime(double jdHigh, double jdLow, double deltaT, GstType gstType, Method method, Accuracy accuracy, ref double gst) {
            return NOVAS_SiderealTime(jdHigh, jdLow, deltaT, gstType, method, accuracy, ref gst);
        }
    }
}