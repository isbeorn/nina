#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.PlateSolving.Solvers;
using NINA.Profile.Interfaces;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class PlateSolverFactoryBehaviorTest {

        private Mock<IPlateSolveSettings> settings;

        [SetUp]
        public void SetUp() {
            settings = new Mock<IPlateSolveSettings>();
            settings.SetupAllProperties();
            settings.Object.AstrometryURL = "https://nova.astrometry.net";
            settings.Object.AstrometryAPIKey = "apikey";
            settings.Object.CygwinLocation = @"C:\cygwin64";
            settings.Object.PS2Location = @"C:\Solver\PlateSolve2.exe";
            settings.Object.PS3Location = @"C:\Solver\PlateSolve3.exe";
            settings.Object.AspsLocation = @"C:\Solver\asps.exe";
            settings.Object.ASTAPLocation = @"C:\Solver\astap.exe";
            settings.Object.TheSkyXHost = "127.0.0.1";
            settings.Object.TheSkyXPort = 3040;
            settings.Object.PinPointCatalogType = Dc3PoinPointCatalogEnum.ppUCAC4;
        }

        private static IEnumerable<TestCaseData> PlateSolverCases() {
            yield return new TestCaseData(PlateSolverEnum.ASTROMETRY_NET, typeof(AstrometryPlateSolver));
            yield return new TestCaseData(PlateSolverEnum.LOCAL, typeof(LocalPlateSolver));
            yield return new TestCaseData(PlateSolverEnum.PLATESOLVE2, typeof(Platesolve2Solver));
            yield return new TestCaseData(PlateSolverEnum.PLATESOLVE3, typeof(Platesolve3Solver));
            yield return new TestCaseData(PlateSolverEnum.ASPS, typeof(AllSkyPlateSolver));
            yield return new TestCaseData(PlateSolverEnum.ASTAP, typeof(ASTAPSolver));
            yield return new TestCaseData(PlateSolverEnum.TSX_IMAGELINK, typeof(TheSkyXImageLinkSolver));
            yield return new TestCaseData(PlateSolverEnum.PINPONT, typeof(Dc3PinPointSolver));
        }

        private static IEnumerable<TestCaseData> BlindSolverCases() {
            yield return new TestCaseData(BlindSolverEnum.ASTROMETRY_NET, typeof(AstrometryPlateSolver));
            yield return new TestCaseData(BlindSolverEnum.LOCAL, typeof(LocalPlateSolver));
            yield return new TestCaseData(BlindSolverEnum.PLATESOLVE3, typeof(Platesolve3Solver));
            yield return new TestCaseData(BlindSolverEnum.ASPS, typeof(AllSkyPlateSolver));
            yield return new TestCaseData(BlindSolverEnum.ASTAP, typeof(ASTAPSolver));
            yield return new TestCaseData(BlindSolverEnum.PINPOINT, typeof(Dc3PinPointSolver));
        }

        /// <summary>
        /// Verifies the plate solver factory maps every configured plate solver enum to the expected concrete integration.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(PlateSolverCases))]
        public void GetPlateSolver_MapsAllPlateSolverTypes(PlateSolverEnum solverType, Type expectedType) {
            settings.Object.PlateSolverType = solverType;

            IPlateSolver solver = PlateSolverFactory.GetPlateSolver(settings.Object);

            solver.Should().BeOfType(expectedType);
        }

        /// <summary>
        /// Verifies the blind solver factory maps every blind solver enum through the supported solver implementations.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(BlindSolverCases))]
        public void GetBlindSolver_MapsAllBlindSolverTypes(BlindSolverEnum solverType, Type expectedType) {
            settings.Object.BlindSolverType = solverType;

            IPlateSolver solver = PlateSolverFactory.GetBlindSolver(settings.Object);

            solver.Should().BeOfType(expectedType);
        }

        /// <summary>
        /// Verifies the DI-facing factory proxy constructs the orchestration services without replacing the supplied dependencies.
        /// </summary>
        [Test]
        public void PlateSolverFactoryProxy_ConstructsOrchestrators() {
            var proxy = new PlateSolverFactoryProxy();
            IPlateSolver plateSolver = Mock.Of<IPlateSolver>();
            IPlateSolver blindSolver = Mock.Of<IPlateSolver>();
            IImagingMediator imagingMediator = Mock.Of<IImagingMediator>();
            IFilterWheelMediator filterWheelMediator = Mock.Of<IFilterWheelMediator>();
            ITelescopeMediator telescopeMediator = Mock.Of<ITelescopeMediator>();
            IDomeMediator domeMediator = Mock.Of<IDomeMediator>();
            IDomeFollower domeFollower = Mock.Of<IDomeFollower>();

            proxy.GetImageSolver(plateSolver, blindSolver).Should().BeOfType<ImageSolver>();
            proxy.GetCaptureSolver(plateSolver, blindSolver, imagingMediator, filterWheelMediator).Should().BeOfType<CaptureSolver>();
            proxy.GetCenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator, filterWheelMediator, domeMediator, domeFollower)
                .Should().BeOfType<CenteringSolver>();
        }
    }
}
