#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Image.ImageAnalysis {

    public interface IContrastDetection {

        Task<ContrastDetectionResult> Measure(IRenderedImage image, ContrastDetectionParams p, IProgress<ApplicationStatus> progress, CancellationToken token);
    }

    public class ContrastDetectionParams {
        public StarSensitivityEnum Sensitivity { get; set; }
        public NoiseReductionEnum NoiseReduction { get; set; }
        public ContrastDetectionMethodEnum Method { get; set; }
        public bool UseROI { get; set; }
        public double InnerCropRatio { get; set; }
    }

    public class ContrastDetectionResult {
        public double AverageContrast { get; set; }
        public double ContrastStdev { get; set; }
    }
}