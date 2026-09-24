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
using System.Drawing;

namespace NINA.Test.Image.ImageAnalysis {
    [TestFixture]
    public class CannyProofTests : CannyAndGaussianProofTestSupport {
        [Test]
        [Category(ProofCategory)]
        [TestCaseSource(nameof(NoBlurMathScenarios))]
        public void NoBlurCannyEdgeDetector_MathScenario_MatchesReference(MathScenario scenario) {
            // Each scenario builds one deterministic byte image. No files and no randomness
            // are involved, so a failure can be reproduced from the scenario name alone.
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            // Most scenarios use the full image. The partial-rectangle case passes a smaller
            // ROI so the same pixel pattern also verifies non-zero rectangle offsets.
            Rectangle rect = scenario.Rect ?? new Rectangle(0, 0, scenario.Width, scenario.Height);

            // The core assertion is byte-for-byte equivalence between the reference filter
            // and the optimized implementation for the exact same image and rectangle.
            byte[] expected = RunReferenceNoBlurCanny(sourcePixels, scenario.Width, scenario.Height, rect, scenario.LowThreshold, scenario.HighThreshold);
            byte[] actual = RunOptimizedNoBlurCanny(sourcePixels, scenario.Width, scenario.Height, rect, scenario.LowThreshold, scenario.HighThreshold);

            Assert.That(actual, Is.EqualTo(expected), scenario.Name);
        }

        [Test]
        [Category(ProofCategory)]
        [TestCaseSource(nameof(BlurredCannyMathScenarios))]
        public void BlurredCannyEdgeDetector_MathScenario_MatchesReference(MathScenario scenario) {
            // Blurred Canny adds Gaussian blur in front of the Canny core. These images
            // are still small, but they include enough interior and border pixels for the 5x5 kernel.
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            Rectangle rect = scenario.Rect ?? new Rectangle(0, 0, scenario.Width, scenario.Height);

            // Use the same Gaussian settings on both sides so any difference comes from
            // implementation behavior, not from test setup.
            byte[] expected = RunReferenceBlurredCanny(sourcePixels, scenario.Width, scenario.Height, rect, scenario.LowThreshold, scenario.HighThreshold, DefaultGaussianSigma, DefaultGaussianSize);
            byte[] actual = RunOptimizedBlurredCanny(sourcePixels, scenario.Width, scenario.Height, rect, scenario.LowThreshold, scenario.HighThreshold, DefaultGaussianSigma, DefaultGaussianSize);

            Assert.That(actual, Is.EqualTo(expected), scenario.Name);
        }

        private static IEnumerable<TestCaseData> NoBlurMathScenarios() {
            foreach (MathScenario scenario in EnumerateNoBlurMathScenarios()) {
                yield return new TestCaseData(scenario).SetName(scenario.Name);
            }
        }

        private static IEnumerable<TestCaseData> BlurredCannyMathScenarios() {
            foreach (MathScenario scenario in EnumerateBlurredCannyMathScenarios()) {
                yield return new TestCaseData(scenario).SetName($"Blurred_{scenario.Name}");
            }
        }

        private static IEnumerable<MathScenario> EnumerateNoBlurMathScenarios() {
            // 13x11 is the smallest comfortable Canny proof size used here: it leaves an
            // 11x9 interior after the filter ignores the outer one-pixel border.
            yield return new MathScenario {
                Name = "NoBlur_Math_Orientation0_LinearX",
                Width = 13,
                Height = 11,
                CreatePixels = static (width, height) => CreateLinearPlane(width, height, 96, 6, 0)
            };

            // Same geometry, but only y changes. This isolates vertical orientation behavior
            // without changing image size or threshold setup.
            yield return new MathScenario {
                Name = "NoBlur_Math_Orientation90_LinearY",
                Width = 13,
                Height = 11,
                CreatePixels = static (width, height) => CreateLinearPlane(width, height, 96, 0, 6)
            };

            // Negative y-step gives the diagonal counterpart to the next scenario. The base
            // value prevents clipping so the intended diagonal slope survives across the image.
            yield return new MathScenario {
                Name = "NoBlur_Math_Orientation45_Diagonal",
                Width = 13,
                Height = 11,
                CreatePixels = static (width, height) => CreateLinearPlane(width, height, 132, 5, -5)
            };

            // Positive y-step covers the other diagonal bucket. Keeping the same 13x11 geometry
            // means any difference is from slope, not from border placement.
            yield return new MathScenario {
                Name = "NoBlur_Math_Orientation135_Diagonal",
                Width = 13,
                Height = 11,
                CreatePixels = static (width, height) => CreateLinearPlane(width, height, 80, 5, 5)
            };

            // Larger center geometry for suppression: the square ring gives broad edges and the
            // cross spikes create local maxima and neighboring competitors.
            yield return new MathScenario {
                Name = "NoBlur_Math_RingAndSpikes_Suppression",
                Width = 21,
                Height = 19,
                CreatePixels = CreateRingAndSpikes
            };

            // Stripe on source row 0: the first processed Canny interior row sees the bright
            // row above it, making the maximum-gradient location touch the top of the interior.
            yield return new MathScenario {
                Name = "NoBlur_Math_MaxGradientFirstInteriorRow",
                Width = 13,
                Height = 11,
                CreatePixels = static (width, height) => CreateHorizontalStripe(width, height, row: 0, value: 255)
            };

            // Stripe through the middle: this places the strongest gradient away from borders,
            // proving the max-gradient tracking is not only a top or bottom edge effect.
            yield return new MathScenario {
                Name = "NoBlur_Math_MaxGradientMiddleInteriorRows",
                Width = 13,
                Height = 11,
                CreatePixels = static (width, height) => CreateHorizontalStripe(width, height, row: height / 2, value: 255)
            };

            // Stripe on the last source row: the last processed Canny interior row sees the
            // bright row below it, which complements the first-row stripe case.
            yield return new MathScenario {
                Name = "NoBlur_Math_MaxGradientLastInteriorRow",
                Width = 13,
                Height = 11,
                CreatePixels = static (width, height) => CreateHorizontalStripe(width, height, row: height - 1, value: 255)
            };

            // Connected weak edge uses the deterministic fixture that was built to place weak
            // pixels beside strong pixels, which exercises the "keep weak edge" hysteresis case.
            yield return new MathScenario {
                Name = "NoBlur_Math_HysteresisConnectedWeakEdge",
                Width = 41,
                Height = 39,
                CreatePixels = static (width, height) => DeterministicImageFixtures.CreateHysteresisConnectedWeakEdgeBytes(width, height, DefaultLowThreshold, DefaultHighThreshold)
            };

            // Isolated weak edge uses the companion fixture where weak pixels have no strong
            // neighbor, so hysteresis should reject them.
            yield return new MathScenario {
                Name = "NoBlur_Math_HysteresisIsolatedWeakEdge",
                Width = 41,
                Height = 39,
                CreatePixels = static (width, height) => DeterministicImageFixtures.CreateHysteresisIsolatedWeakEdgeBytes(width, height, DefaultLowThreshold, DefaultHighThreshold)
            };

            // Diagonal bridge keeps a weak edge through diagonal adjacency. This is important
            // because hysteresis checks all eight neighbors, not only left/right/top/bottom.
            yield return new MathScenario {
                Name = "NoBlur_Math_HysteresisDiagonalBridge",
                Width = 41,
                Height = 39,
                CreatePixels = static (width, height) => DeterministicImageFixtures.CreateHysteresisDiagonalBridgeBytes(width, height, DefaultLowThreshold, DefaultHighThreshold)
            };

            // Partial ROI: the rectangle starts at (4,3) and is 17x15, so the filter must handle
            // a non-zero offset, a local Canny border, and untouched pixels outside the rectangle.
            yield return new MathScenario {
                Name = "NoBlur_Math_PartialRectangle",
                Width = 25,
                Height = 23,
                Rect = new Rectangle(4, 3, 17, 15),
                CreatePixels = CreateRingAndSpikes
            };
        }

        private static IEnumerable<MathScenario> EnumerateBlurredCannyMathScenarios() {
            // 17x15 is larger than the 5x5 Gaussian kernel, so blurred Canny includes full-kernel
            // interior pixels before the Canny core runs on the blurred result.
            yield return new MathScenario {
                Name = "Math_LinearX",
                Width = 17,
                Height = 15,
                CreatePixels = static (width, height) => CreateLinearPlane(width, height, 84, 5, 1)
            };

            // The ring-and-spikes pattern after blur still has structured edges for Canny, but
            // Gaussian smoothing also sees many non-uniform kernel neighborhoods.
            yield return new MathScenario {
                Name = "Math_RingAndSpikes",
                Width = 25,
                Height = 23,
                CreatePixels = CreateRingAndSpikes
            };

            // 3x3 is smaller than a radius-2 Gaussian kernel can fully cover. This keeps the
            // blurred Canny path honest for tiny images before the Canny rectangle is processed.
            yield return new MathScenario {
                Name = "Math_TinyImage_AllGaussianPixelsAreBorder",
                Width = 3,
                Height = 3,
                CreatePixels = static (width, height) => CreateLinearPlane(width, height, 84, 5, 1)
            };
        }
    }
}
