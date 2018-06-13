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
        public static IPlateSolver CreateInstance(IProfileService profileService, PlateSolverEnum solver, int binning, double width, double height, Coordinates coords = null) {
            IPlateSolver Platesolver = null;

            if (solver == PlateSolverEnum.ASTROMETRY_NET) {
                Platesolver = new AstrometryPlateSolver(ASTROMETRYNETURL, profileService.ActiveProfile.PlateSolveSettings.AstrometryAPIKey);
            } else if (solver == PlateSolverEnum.LOCAL) {
                if (profileService.ActiveProfile.PlateSolveSettings.SearchRadius > 0 && coords != null) {
                    Platesolver = new LocalPlateSolver(
                        profileService.ActiveProfile.TelescopeSettings.FocalLength,
                        profileService.ActiveProfile.CameraSettings.PixelSize * binning,
                        profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                        coords,
                        profileService.ActiveProfile.PlateSolveSettings.CygwinLocation
                    );
                } else {
                    Platesolver = new LocalPlateSolver(
                        profileService.ActiveProfile.TelescopeSettings.FocalLength,
                        profileService.ActiveProfile.CameraSettings.PixelSize * binning,
                        profileService.ActiveProfile.PlateSolveSettings.CygwinLocation
                    );
                }
            } else if (solver == PlateSolverEnum.PLATESOLVE2) {
                if (coords == null) {
                    Notification.ShowWarning(Locale.Loc.Instance["LblPlatesolve2NoCoordinates"]);
                }
                Platesolver = new Platesolve2Solver(
                    profileService.ActiveProfile.TelescopeSettings.FocalLength,
                    profileService.ActiveProfile.CameraSettings.PixelSize * binning,
                    width,
                    height,
                    profileService.ActiveProfile.PlateSolveSettings.Regions,
                    coords,
                    profileService.ActiveProfile.PlateSolveSettings.PS2Location
                );
            }

            return Platesolver;
        }
    }
}