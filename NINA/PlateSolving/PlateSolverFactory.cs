using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.PlateSolving
{
    static class PlateSolverFactory
    {
        public const string ASTROMETRYNETURL = "http://nova.astrometry.net";

        /// <summary>
        /// Creates an instance of a Platesolver depending on the PlatesolverType that is defined inside the settings
        /// </summary>
        /// <param name="binning">Camera binning</param>
        /// <param name="width">Width of the image</param>
        /// <param name="height">Height of the image</param>
        /// <param name="coords">Expected Coordinates of the image center</param>
        /// <returns></returns>
        public static IPlateSolver CreateInstance(int binning, double width, double height, Coordinates coords = null) {
            IPlateSolver Platesolver = null;

            if (Settings.PlateSolverType == PlateSolverEnum.ASTROMETRY_NET) {
                Platesolver = new AstrometryPlateSolver(ASTROMETRYNETURL, Settings.AstrometryAPIKey);
            } else if (Settings.PlateSolverType == PlateSolverEnum.LOCAL) {
                if (Settings.AnsvrSearchRadius > 0 && coords != null) {
                    Platesolver = new LocalPlateSolver(Settings.AnsvrFocalLength, Settings.AnsvrPixelSize * binning, Settings.AnsvrSearchRadius, coords);
                } else {
                    Platesolver = new LocalPlateSolver(Settings.AnsvrFocalLength, Settings.AnsvrPixelSize * binning);
                }
            } else if (Settings.PlateSolverType == PlateSolverEnum.PLATESOLVE2) {                  
                if(coords == null) {
                    Notification.ShowError("No coordinates available. Platesolve2 requires coordinates to solve!");
                }
                Platesolver = new Platesolve2Solver(Settings.PS2FocalLength, Settings.PS2PixelSize * binning, width, height, Settings.PS2Regions, coords);
            }

            return Platesolver;
        }
    }
}
