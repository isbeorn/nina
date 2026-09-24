#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NUnit.Framework;
using System.Collections.Generic;

namespace NINA.Test.Image.ImageAnalysis {
    [TestFixture]
    public class GaussianProofTests : CannyAndGaussianProofTestSupport {
        [Test]
        [Category(ProofCategory)]
        [TestCaseSource(nameof(GaussianMathScenarios))]
        public void GaussianBlur_MathScenario_MatchesReference(MathScenario scenario) {
            // Each Gaussian scenario builds one deterministic byte image. The reference and
            // optimized filters receive the same unmanaged 8bpp image shape.
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            // Byte-for-byte equality includes the output image buffer returned by each filter.
            byte[] expected = RunReferenceGaussianBlur(sourcePixels, scenario.Width, scenario.Height, DefaultGaussianSigma, DefaultGaussianSize);
            byte[] actual = RunOptimizedGaussianBlur(sourcePixels, scenario.Width, scenario.Height, DefaultGaussianSigma, DefaultGaussianSize);

            Assert.That(actual, Is.EqualTo(expected), scenario.Name);
        }

        private static IEnumerable<TestCaseData> GaussianMathScenarios() {
            foreach (MathScenario scenario in EnumerateGaussianMathScenarios()) {
                yield return new TestCaseData(scenario).SetName($"Gaussian_{scenario.Name}");
            }
        }

        private static IEnumerable<MathScenario> EnumerateGaussianMathScenarios() {
            // 17x15 is larger than the 5x5 Gaussian kernel, so it includes full-kernel interior
            // pixels and every border/corner shape in one simple ramp.
            yield return new MathScenario {
                Name = "Math_LinearX",
                Width = 17,
                Height = 15,
                CreatePixels = static (width, height) => CreateLinearPlane(width, height, 84, 5, 1)
            };

            // The ring-and-spikes pattern gives Gaussian smoothing many non-uniform kernel
            // neighborhoods while staying deterministic and easy to reproduce.
            yield return new MathScenario {
                Name = "Math_RingAndSpikes",
                Width = 25,
                Height = 23,
                CreatePixels = CreateRingAndSpikes
            };

            // 3x3 is smaller than a radius-2 Gaussian kernel can fully cover. Every output pixel
            // is therefore a border/corner-style Gaussian sample.
            yield return new MathScenario {
                Name = "Math_TinyImage_AllGaussianPixelsAreBorder",
                Width = 3,
                Height = 3,
                CreatePixels = static (width, height) => CreateLinearPlane(width, height, 84, 5, 1)
            };
        }

    }
}
