﻿#region "copyright"

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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NINA.Utility.Astrometry;

namespace NINA.PlateSolving {

    internal class ASTAPSolver : CLISolver {

        public ASTAPSolver(string executableLocation)
            : base(executableLocation) {
        }

        protected override PlateSolveResult ReadResult(
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            var result = new PlateSolveResult() { Success = false };
            if (File.Exists(outputFilePath)) {
                var dict = File.ReadLines(outputFilePath)
                   .Where(line => !string.IsNullOrWhiteSpace(line))
                   .Select(line => line.Split(new char[] { '=' }, 2, 0))
                   .ToDictionary(parts => parts[0], parts => parts[1]);
                if (dict.ContainsKey("PLTSOLVD")) {
                    result.Success = dict["PLTSOLVD"] == "T" ? true : false;

                    if (result.Success) {
                        result.Coordinates = new Coordinates(
                            double.Parse(dict["CRVAL1"], CultureInfo.InvariantCulture),
                            double.Parse(dict["CRVAL2"], CultureInfo.InvariantCulture),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees
                        );
                        result.Orientation = double.Parse(dict["CROTA2"], CultureInfo.InvariantCulture);
                        result.Pixscale = imageProperties.ArcSecPerPixel;
                    }
                }
            }
            return result;
        }

        protected override string GetLocalizedPlateSolverName() {
            return Locale.Loc.Instance["LblASTAPNotFound"];
        }

        /// <summary>
        /// Creates the arguments to launch ASTAP process
        /// </summary>
        /// <returns></returns>
        /// <remarks>http://www.hnsky.org/astap.htm#astap_command_line</remarks>
        protected override string GetArguments(
            string imageFilePath,
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            var args = new List<string>();

            //File location to solve
            args.Add($"-f \"{imageFilePath}\"");

            //Field height of image
            var fov = Math.Round(imageProperties.FoVH, 6);
            args.Add($"-fov {fov.ToString(CultureInfo.InvariantCulture)}");

            //Downsample factor
            args.Add($"-z {parameter.DownSampleFactor}");

            //Max number of stars
            args.Add($"-s {parameter.MaxObjects}");

            if (parameter.SearchRadius > 0 && parameter.Coordinates != null) {
                //Search field radius
                args.Add($"-r {parameter.SearchRadius}");

                var ra = Math.Round(parameter.Coordinates.RA, 6);
                //Right Ascension in degrees
                args.Add($"-ra {ra.ToString(CultureInfo.InvariantCulture)}");

                var spd = Math.Round(parameter.Coordinates.Dec + 90.0, 6);
                //South pole distance in degrees
                args.Add($"-spd {spd.ToString(CultureInfo.InvariantCulture)}");
            } else {
                //Search field radius
                args.Add($"-r {180}");
            }

            return string.Join(" ", args);
        }

        protected override string GetOutputPath(string imageFilePath) {
            return Path.Combine(Path.GetDirectoryName(imageFilePath), Path.GetFileNameWithoutExtension(imageFilePath)) + ".ini";
        }
    }
}