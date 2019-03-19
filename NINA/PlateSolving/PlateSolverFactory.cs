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
            } else if (solver == PlateSolverEnum.ASPS) {
                if (profileService.ActiveProfile.PlateSolveSettings.SearchRadius > 0 && coords != null) {
                    Platesolver = new AllSkyPlateSolver(
                        profileService.ActiveProfile.TelescopeSettings.FocalLength,
                        profileService.ActiveProfile.CameraSettings.PixelSize * binning,
                        profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                        coords,
                        profileService.ActiveProfile.PlateSolveSettings.AspsLocation
                    );
                } else {
                    Platesolver = new AllSkyPlateSolver(
                        profileService.ActiveProfile.TelescopeSettings.FocalLength,
                        profileService.ActiveProfile.CameraSettings.PixelSize * binning,
                        profileService.ActiveProfile.PlateSolveSettings.AspsLocation
                    );
                }
            } else if (solver == PlateSolverEnum.ASTAP) {
                if (profileService.ActiveProfile.PlateSolveSettings.SearchRadius > 0 && coords != null) {
                    Platesolver = new ASTAPSolver(
                        profileService.ActiveProfile.TelescopeSettings.FocalLength,
                        profileService.ActiveProfile.CameraSettings.PixelSize * binning,
                        width,
                        profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                        coords,
                        profileService.ActiveProfile.PlateSolveSettings.ASTAPLocation
                    );
                } else {
                    Platesolver = new ASTAPSolver(
                        profileService.ActiveProfile.TelescopeSettings.FocalLength,
                        profileService.ActiveProfile.CameraSettings.PixelSize * binning,
                        width,
                        profileService.ActiveProfile.PlateSolveSettings.ASTAPLocation
                    );
                }
            }

            return Platesolver;
        }
    }
}