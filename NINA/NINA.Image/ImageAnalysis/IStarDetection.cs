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
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Image.ImageAnalysis {

    public interface IStarDetection : IPluggableBehavior<IStarDetection> {

        Task<StarDetectionResult> Detect(IRenderedImage image, System.Windows.Media.PixelFormat pf, StarDetectionParams p, IProgress<ApplicationStatus> progress, CancellationToken token);

        IStarDetectionAnalysis CreateAnalysis();

        void UpdateAnalysis(IStarDetectionAnalysis analysis, StarDetectionParams p, StarDetectionResult result);
    }

    public class StarDetectionParams {
        public StarSensitivityEnum Sensitivity { get; set; }
        public NoiseReductionEnum NoiseReduction { get; set; }
        public bool IsAutoFocus { get; set; }
        public bool UseROI { get; set; } = false;
        public double InnerCropRatio { get; set; } = 1.0d;
        public double OuterCropRatio { get; set; } = 1.0d;
        public int NumberOfAFStars { get; set; } = 0;
        public List<Accord.Point> MatchStarPositions { get; set; } = new List<Accord.Point>();
    }

    public class DetectedStar {
        public double HFR { get; set; }
        public Accord.Point Position { get; set; }
        public double AverageBrightness { get; set; }
        public double MaxBrightness { get; set; }
        public double Background { get; set; }
        public Rectangle BoundingBox { get; set; }
    }

    public class StarDetectionResult {
        public double AverageHFR { get; set; }
        public int DetectedStars { get; set; }
        public double HFRStdDev { get; set; }
        public List<DetectedStar> StarList { get; set; }
        public List<Accord.Point> BrightestStarPositions { get; set; }
        public StarDetectionParams Params { get; set; }
    }
}