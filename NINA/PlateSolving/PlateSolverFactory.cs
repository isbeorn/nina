using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Notification;
using NINA.Utility.Profile;

namespace NINA.PlateSolving {

    internal static class PlateSolverFactory {
        public const string ASTROMETRYNETURL = "http://nova.astrometry.net";

        /// <summary>
        /// Creates an instance of a Platesolver depending on the solver
        /// </summary>
        /// <param name="solver"> Plate Solver that should be used</param>
        /// <param name="binning">Camera binning</param>
        /// <param name="width">  Width of the image</param>
        /// <param name="height"> Height of the image</param>
        /// <param name="coords"> Expected Coordinates of the image center</param>
        /// <returns></returns>
        public static IPlateSolver CreateInstance(PlateSolverEnum solver, int binning, double width, double height, Coordinates coords = null) {
            IPlateSolver Platesolver = null;

            if (solver == PlateSolverEnum.ASTROMETRY_NET) {
                Platesolver = new AstrometryPlateSolver(ASTROMETRYNETURL, ProfileManager.Instance.ActiveProfile.PlateSolveSettings.AstrometryAPIKey);
            } else if (solver == PlateSolverEnum.LOCAL) {
                if (ProfileManager.Instance.ActiveProfile.PlateSolveSettings.SearchRadius > 0 && coords != null) {
                    Platesolver = new LocalPlateSolver(ProfileManager.Instance.ActiveProfile.TelescopeSettings.FocalLength, ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize * binning, ProfileManager.Instance.ActiveProfile.PlateSolveSettings.SearchRadius, coords);
                } else {
                    Platesolver = new LocalPlateSolver(ProfileManager.Instance.ActiveProfile.TelescopeSettings.FocalLength, ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize * binning);
                }
            } else if (solver == PlateSolverEnum.PLATESOLVE2) {
                if (coords == null) {
                    Notification.ShowWarning(Locale.Loc.Instance["LblPlatesolve2NoCoordinates"]);
                }
                Platesolver = new Platesolve2Solver(ProfileManager.Instance.ActiveProfile.TelescopeSettings.FocalLength, ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize * binning, width, height, ProfileManager.Instance.ActiveProfile.PlateSolveSettings.Regions, coords);
            }

            return Platesolver;
        }
    }
}