#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Locale;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NINA.PlateSolving.Solvers {

    internal class Platesolve3Solver : CLISolver {

        public Platesolve3Solver(string executableLocation)
            : base(executableLocation) {
            this.executableLocation = executableLocation;
        }

        /// <summary>
        /// Platesolve3.80 can be executed from the command line. The command line parameters are:
        ///
        ///        FileName -- the full file name of the image to be platesolved
        ///        RA	 -- the estimated RA of the center of the image, in radians
        ///        Dec	 -- the estimated Dec of the center of the image, in radians
        ///        Xsize	 -- the estimated width of the image, in radians
        ///        Ysize	 -- the estimatesd height of the image, in radians
        ///
        ///        The call syntax is:
        ///        platesolve3.80 Filename RA Dec Xsize Ysize
        ///
        ///        Here's an example of a call:
        ///        platesolve3.80 "C:\Users\13108\Documents\PlateSolve3\Platesolve3.80\M45-0004B.fit" 0.9905 0.423 0.214 0.214
        ///
        /// </summary>
        /// <returns></returns>
        protected override string GetArguments(
            string imageFilePath,
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            var args = new List<string>();
            args.Add("\"" + imageFilePath + "\"");
            if (parameter.Coordinates == null) {
                args.Add("0");
                args.Add("0");
            } else {
                args.Add(AstroUtil.ToRadians(parameter.Coordinates.RADegrees).ToString(CultureInfo.InvariantCulture));
                args.Add(AstroUtil.ToRadians(parameter.Coordinates.Dec).ToString(CultureInfo.InvariantCulture));
            }
            args.Add(AstroUtil.ToRadians(imageProperties.FoVW).ToString(CultureInfo.InvariantCulture));
            args.Add(AstroUtil.ToRadians(imageProperties.FoVH).ToString(CultureInfo.InvariantCulture));
            return string.Join(" ", args);
        }

        /// <summary>
        /// The platesolve results are placed in the same folder as the image. It will have the same name as the image file with new extention, _PS3.txt The output file is organized as follows:
        ///
        ///        Platesolve_was_successful       	 -- True or False
        ///        RA, Dec			   		 -- RA(J2000) in radians, Dec(J2000) in radians
        ///        Imscale, Rot		   		 -- image scale in pixels/radian, Rotation angle in degrees from North.
        ///        Match_Method		   		 -- method used to match the image(string)
        ///        A,B,C,D,Alpha,Beta,Theta,Gamma  	 -- these output parameters define the geometric transformation of the image to the tangent plane of the celestial sphere centered on RA,Dec.
        ///        U0,V0					 --
        ///        MagOffset,WRMS,AveBG,NumPlate,NumMatched --
        ///
        /// </summary>
        /// <returns>PlateSolveResult</returns>
        protected override PlateSolveResult ReadResult(
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            PlateSolveResult result = new PlateSolveResult() { Success = false };
            if (File.Exists(outputFilePath)) {
                using (var s = new StreamReader(outputFilePath)) {
                    string line;
                    int linenr = 0;
                    while ((line = s.ReadLine()) != null) {
                        if (linenr == 0 && line.ToLower() != "true") {
                            // If platesolving succeeds it will start with a line containing True
                            return result;
                        }
                        string[] resultArr = line.Split(',');
                        if (linenr == 1) {
                            if (resultArr.Length >= 2) {
                                double ra = double.Parse(resultArr[0], CultureInfo.InvariantCulture);
                                double dec = double.Parse(resultArr[1], CultureInfo.InvariantCulture);

                                /* success */
                                result.Success = true;
                                result.Coordinates = new Coordinates(AstroUtil.ToDegree(ra), AstroUtil.ToDegree(dec), Epoch.J2000, Coordinates.RAType.Degrees);
                            }
                        }
                        if (linenr == 2) {
                            if (resultArr.Length >= 2) {
                                result.Pixscale = 206264.8d / double.Parse(resultArr[0], CultureInfo.InvariantCulture);
                                if (!double.IsNaN(result.Pixscale)) {
                                    result.Radius = AstroUtil.ArcsecToDegree(Math.Sqrt(Math.Pow(imageProperties.ImageWidth * result.Pixscale, 2) + Math.Pow(imageProperties.ImageHeight * result.Pixscale, 2)) / 2d);
                                }
                                result.PositionAngle = double.Parse(resultArr[1], CultureInfo.InvariantCulture);
                            }
                        }
                        if (linenr == 4) {
                            if (resultArr.Length >= 2) {
                                result.Flipped = double.Parse(resultArr[0], CultureInfo.InvariantCulture) >= 0;
                            }
                        }
                        linenr++;
                    }
                }
            }
            return result;
        }

        protected override string GetLocalizedPlateSolverName() {
            return Loc.Instance["LblPlatesolve3NotFound"];
        }

        protected override string GetOutputPath(string imageFilePath) {
            return Path.Combine(Path.GetDirectoryName(imageFilePath), Path.GetFileNameWithoutExtension(imageFilePath)) + "_PS3.txt";
        }
    }
}