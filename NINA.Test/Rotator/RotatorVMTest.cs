#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;

namespace NINA.Test.Rotator {

    [TestFixture]
    public class RotatorVMTest {
        private Mock<IProfileService> mockProfileService;
        private Mock<IDeviceChooserVM> mockRotatorDeviceChooserVM;
        private Mock<IApplicationStatusMediator> mockApplicationStatusMediator;
        private Mock<IApplicationResourceDictionary> mockResourceDictionary;
        private Mock<IRotatorMediator> mockRotatorMediator;
        private Mock<IRotator> mockRotator;

        private string rotatorId;
        private bool rotatorConnected;
        private bool isSynced;
        private float offset;
        private float mechanicalPosition;
        private RotatorRangeTypeEnum rangeType;
        private float rangeStartMechanicalPosition;

        [SetUp]
        public void Init() {
            rotatorId = "ID";
            rotatorConnected = false;
            isSynced = false;
            offset = 0.0f;
            mechanicalPosition = 0.0f;
            rangeType = RotatorRangeTypeEnum.FULL;
            rangeStartMechanicalPosition = 0.0f;

            mockRotatorDeviceChooserVM = new Mock<IDeviceChooserVM>();
            mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            mockRotatorMediator = new Mock<IRotatorMediator>();
            mockProfileService = new Mock<IProfileService>();
            mockResourceDictionary = new Mock<IApplicationResourceDictionary>();
            mockProfileService.SetupProperty(p => p.ActiveProfile.RotatorSettings.Id);
            mockProfileService.SetupGet(p => p.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(0);
            mockProfileService.SetupGet(p => p.ActiveProfile.RotatorSettings.RangeType).Returns(() => rangeType);
            mockProfileService.SetupGet(p => p.ActiveProfile.RotatorSettings.RangeStartMechanicalPosition).Returns(() => rangeStartMechanicalPosition);
            mockRotator = new Mock<IRotator>();
        }

        private async Task<RotatorVM> CreateSUT() {
            var rotatorVM = new RotatorVM(mockProfileService.Object, mockRotatorMediator.Object, mockRotatorDeviceChooserVM.Object, mockResourceDictionary.Object, mockApplicationStatusMediator.Object);

            mockRotator = new Mock<IRotator>();
            mockRotator.SetupGet(x => x.Id).Returns(() => rotatorId);
            mockRotator.SetupGet(x => x.Connected).Returns(() => rotatorConnected);
            mockRotator.SetupGet(x => x.Synced).Returns(() => isSynced);
            mockRotator.SetupGet(x => x.MechanicalPosition).Returns(() => mechanicalPosition);
            mockRotator.SetupGet(x => x.IsMoving).Returns(false);
            mockRotator.SetupGet(x => x.Position).Returns(() => mechanicalPosition + offset);
            mockRotator.Setup(x => x.Move(It.IsAny<float>(), It.IsAny<CancellationToken>())).Callback<float, CancellationToken>((requestedPosition, ct) => {
                mechanicalPosition = AstroUtil.EuclidianModulus(mechanicalPosition + requestedPosition + 360, 360);
            });
            mockRotator.Setup(x => x.MoveAbsolute(It.IsAny<float>(), It.IsAny<CancellationToken>())).Callback<float, CancellationToken>((requestedPosition, ct) => {
                mechanicalPosition = AstroUtil.EuclidianModulus(requestedPosition - offset + 360, 360);
            });
            mockRotator.Setup(x => x.MoveAbsoluteMechanical(It.IsAny<float>(), It.IsAny<CancellationToken>())).Callback<float, CancellationToken>((requestedPosition, ct) => {
                mechanicalPosition = AstroUtil.EuclidianModulus(requestedPosition, 360);
            });

            mockRotator.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Callback<CancellationToken>(ct => {
                rotatorConnected = true;
            }).ReturnsAsync(true);

            mockRotatorDeviceChooserVM.SetupGet(x => x.SelectedDevice).Returns(mockRotator.Object);
            mockRotatorDeviceChooserVM.SetupGet(x => x.Devices).Returns(new List<IDevice>());

            var connectionResult = await rotatorVM.Connect();
            Assert.That(connectionResult, Is.True);
            return rotatorVM;
        }

        /// <summary>
        /// Verifies that a successful rotator connection broadcasts the populated connected info snapshot.
        /// This protects mediator consumers that need the connected rotator state immediately after Connect completes.
        /// </summary>
        [Test]
        public async Task Test_Connect_BroadcastsConnectedInfo() {
            await CreateSUT();

            mockRotatorMediator.Verify(x => x.Broadcast(It.Is<RotatorInfo>(info => info.Connected && info.DeviceId == rotatorId)), Times.AtLeastOnce);
        }

        [Test]
        public async Task Test_MovePosition_NotSynced_Throws() {
            var sut = await CreateSUT();

            Assert.ThrowsAsync<Exception>(async () => await sut.Move(1.0f, CancellationToken.None), message: "Rotator not synced!");
        }

        [Test]
        public async Task Test_GetTargetPosition_NotSynced_Throws() {
            var sut = await CreateSUT();

            Assert.Throws<Exception>(() => sut.GetTargetPosition(1.0f), message: "Rotator not synced!");
        }

        [Test]
        [TestCase(15.0f, 15.0f)]
        [TestCase(90.9f, 90.9f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(195.0f, 15.0f, RotatorRangeTypeEnum.HALF, 1.0f)]
        public async Task Test_MoveMechanical(float requestedPosition, float expectedPosition, RotatorRangeTypeEnum rangeType = RotatorRangeTypeEnum.FULL, float rangeStartMechanicalPosition = 0.0f) {
            var sut = await CreateSUT();
            this.rangeType = rangeType;
            this.rangeStartMechanicalPosition = rangeStartMechanicalPosition;
            isSynced = true;
            mechanicalPosition = 10.0f;
            offset = 5.0f;

            var cts = new CancellationTokenSource();
            var result = await sut.MoveMechanical(requestedPosition, TimeSpan.Zero, cts.Token);
            Assert.That(result, Is.EqualTo(expectedPosition));
            mockRotator.Verify(x => x.MoveAbsoluteMechanical(expectedPosition, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(5.0f, 15.0f)]
        [TestCase(80.9f, 90.9f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(185.0f, 15.0f, RotatorRangeTypeEnum.HALF, 1.0f)]
        public async Task Test_MoveRelativePosition(float requestedAmount, float expectedPosition, RotatorRangeTypeEnum rangeType = RotatorRangeTypeEnum.FULL, float rangeStartMechanicalPosition = 0.0f) {
            var sut = await CreateSUT();
            this.rangeType = rangeType;
            this.rangeStartMechanicalPosition = rangeStartMechanicalPosition;
            isSynced = true;
            mechanicalPosition = 10.0f;
            offset = 5.0f;

            var cts = new CancellationTokenSource();
            var result = await sut.MoveRelative(requestedAmount, TimeSpan.Zero, cts.Token);
            Assert.That(result, Is.EqualTo(expectedPosition));
            mockRotator.Verify(x => x.MoveAbsoluteMechanical(expectedPosition, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(10.0f, 10.0f)]
        [TestCase(100.0f, 280.0f)]  // Optimized: reciprocal 280° is closer from 0° than direct 100°
        [TestCase(190.0f, 10.0f)]   // Optimized: reciprocal 10° is closer from 0° than direct 190°
        [TestCase(280.0f, 280.0f)]
        // Mechanical range is 1-181, and Position range is 6-186
        [TestCase(5.0f, 185.0f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(10.0f, 10.0f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(100.0f, 100.0f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(185.9f, 185.9f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(186.1f, 6.1f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(280.0f, 100.0f, RotatorRangeTypeEnum.HALF, 1.0f)]
        // Mechanical range is 270-90, and Position range is 275-95
        [TestCase(274.9f, 94.9f, RotatorRangeTypeEnum.HALF, 270.0f)]
        [TestCase(275.1f, 275.1f, RotatorRangeTypeEnum.HALF, 270.0f)]
        [TestCase(94.9f, 94.9f, RotatorRangeTypeEnum.HALF, 270.0f)]
        [TestCase(95.1f, 275.1f, RotatorRangeTypeEnum.HALF, 270.0f)]
        // Mechanical range is 1-91, and Position range is 6-96
        [TestCase(5.0f, 95.0f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(10.0f, 10.0f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(95.9f, 95.9f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(96.1f, 6.1f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(185.9f, 95.9f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(186.1f, 6.1f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(275.9f, 95.9f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(276.1f, 6.1f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        public async Task Test_GetPosition_RangeTypes(float requestedPosition, float expectedPosition, RotatorRangeTypeEnum rangeType = RotatorRangeTypeEnum.FULL, float rangeStartMechanicalPosition = 0.0f) {
            var sut = await CreateSUT();
            this.rangeType = rangeType;
            this.rangeStartMechanicalPosition = rangeStartMechanicalPosition;
            isSynced = true;
            offset = 5.0f;

            var result = sut.GetTargetPosition(requestedPosition);
            Assert.That(result, Is.EqualTo(expectedPosition).Within(0.1));
        }

        [Test]
        [TestCase(10.0f, 10.0f, 0.0f, Description = "LITERAL with offset, exact position")]
        [TestCase(100.0f, 100.0f, 5.0f, Description = "LITERAL with offset, exact position")]
        [TestCase(190.0f, 190.0f, -10.0f, Description = "LITERAL with negative offset")]
        [TestCase(350.0f, 350.0f, 20.0f, Description = "LITERAL near 360° with offset")]
        public async Task Test_GetPosition_LiteralRange(float requestedPosition, float expectedPosition, float offsetValue) {
            var sut = await CreateSUT();
            rangeType = RotatorRangeTypeEnum.LITERAL;
            rangeStartMechanicalPosition = 0.0f;
            isSynced = true;
            mechanicalPosition = 100.0f;
            offset = offsetValue;

            var result = sut.GetTargetPosition(requestedPosition);
            Assert.That(result, Is.EqualTo(expectedPosition).Within(0.1));
        }

        [Test]
        [TestCase(5.0f, 5.0f, 0.0f, 0.0f, Description = "FULL range, same quadrant optimization")]
        [TestCase(185.0f, 5.0f, 0.0f, 0.0f, Description = "FULL range, 180° reciprocal optimization")]
        [TestCase(10.0f, 10.0f, 5.0f, 0.0f, Description = "FULL range with offset, direct")]
        [TestCase(190.0f, 10.0f, 5.0f, 10.0f, Description = "FULL range with offset, reciprocal optimization")]
        [TestCase(245.0f, 245.0f, 0.0f, 245.0f, Description = "FULL range, same position")]
        public async Task Test_GetPosition_FullRange(float requestedPosition, float expectedPosition, float offsetValue, float currentMechanicalPosition) {
            var sut = await CreateSUT();
            rangeType = RotatorRangeTypeEnum.FULL;
            rangeStartMechanicalPosition = 0.0f;
            isSynced = true;
            mechanicalPosition = currentMechanicalPosition;
            offset = offsetValue;

            var result = sut.GetTargetPosition(requestedPosition);
            Assert.That(result, Is.EqualTo(expectedPosition).Within(0.1));
        }

        [Test]
        [TestCase(10.0f, 10.0f)]
        [TestCase(100.0f, 280.0f)]  // Optimized: reciprocal 280° is closer from 0° than direct 100°
        [TestCase(190.0f, 10.0f)]   // Optimized: reciprocal 10° is closer from 0° than direct 190°
        [TestCase(280.0f, 280.0f)]
        // Mechanical range is 1-181, and Position range is 6-186
        [TestCase(10.0f, 10.0f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(100.0f, 100.0f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(180.9f, 180.9f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(181.1f, 1.1f, RotatorRangeTypeEnum.HALF, 1.0f)]
        [TestCase(280.0f, 100.0f, RotatorRangeTypeEnum.HALF, 1.0f)]
        // Mechanical range is 270-90, and Position range is 275-95
        [TestCase(269.9f, 89.9f, RotatorRangeTypeEnum.HALF, 270.0f)]
        [TestCase(270.1f, 270.1f, RotatorRangeTypeEnum.HALF, 270.0f)]
        [TestCase(89.9f, 89.9f, RotatorRangeTypeEnum.HALF, 270.0f)]
        [TestCase(90.1f, 270.1f, RotatorRangeTypeEnum.HALF, 270.0f)]
        // Mechanical range is 1-91, and Position range is 6-96
        [TestCase(0.9f, 90.9f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(1.1f, 1.1f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(90.9f, 90.9f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(91.1f, 1.1f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(180.9f, 90.9f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(181.1f, 1.1f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(270.9f, 90.9f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        [TestCase(271.1f, 1.1f, RotatorRangeTypeEnum.QUARTER, 1.0f)]
        public async Task Test_GetMechanicalPosition_RangeTypes(float requestedPosition, float expectedPosition, RotatorRangeTypeEnum rangeType = RotatorRangeTypeEnum.FULL, float rangeStartMechanicalPosition = 0.0f) {
            var sut = await CreateSUT();
            this.rangeType = rangeType;
            this.rangeStartMechanicalPosition = rangeStartMechanicalPosition;
            isSynced = true;
            offset = 5.0f;

            // Update RotatorInfo to reflect the current mechanical position
            sut.RotatorInfo.MechanicalPosition = mechanicalPosition;

            var result = sut.GetTargetMechanicalPosition(requestedPosition);
            Assert.That(result, Is.EqualTo(expectedPosition).Within(0.1));
        }

        [Test]
        [TestCase(10.0f, 10.0f, Description = "Exact position requested")]
        [TestCase(100.0f, 100.0f, Description = "Exact position requested")]
        [TestCase(190.0f, 190.0f, Description = "Exact position requested, no optimization")]
        [TestCase(280.0f, 280.0f, Description = "Exact position requested")]
        [TestCase(0.0f, 0.0f, Description = "Zero position")]
        [TestCase(359.9f, 359.9f, Description = "Near 360°")]
        public async Task Test_GetMechanicalPosition_LiteralRange(float requestedPosition, float expectedPosition) {
            var sut = await CreateSUT();
            rangeType = RotatorRangeTypeEnum.LITERAL;
            rangeStartMechanicalPosition = 0.0f;
            isSynced = true;
            mechanicalPosition = 100.0f;
            offset = 0.0f;

            var result = sut.GetTargetMechanicalPosition(requestedPosition);
            Assert.That(result, Is.EqualTo(expectedPosition).Within(0.1));
        }

        [Test]
        [TestCase(10.0f, 10.0f, 0.0f, Description = "Same quadrant, direct movement")]
        [TestCase(100.0f, 100.0f, 90.0f, Description = "Different quadrant, direct movement")]
        [TestCase(190.0f, 10.0f, 10.0f, Description = "Optimized via 180° reciprocal (190° from 10° is shorter than 10° from 10°)")]
        [TestCase(280.0f, 280.0f, 270.0f, Description = "Close to current position")]
        [TestCase(180.0f, 0.0f, 10.0f, Description = "180° reciprocal optimization (180° away becomes 0°)")]
        [TestCase(0.0f, 0.0f, 10.0f, Description = "Direct movement (0° is closer than reciprocal 180°)")]
        [TestCase(270.0f, 90.0f, 100.0f, Description = "Reciprocal when closer (270° from 100° via 90° reciprocal)")]
        [TestCase(90.0f, 90.0f, 100.0f, Description = "Direct movement (90° is closer than reciprocal 270°)")]
        public async Task Test_GetMechanicalPosition_FullRange_OptimizesMovement(float requestedPosition, float expectedPosition, float currentMechanicalPosition) {
            var sut = await CreateSUT();
            rangeType = RotatorRangeTypeEnum.FULL;
            rangeStartMechanicalPosition = 0.0f;
            isSynced = true;
            mechanicalPosition = currentMechanicalPosition;
            offset = 0.0f;

            // Update RotatorInfo to reflect the current mechanical position
            sut.RotatorInfo.MechanicalPosition = currentMechanicalPosition;

            var result = sut.GetTargetMechanicalPosition(requestedPosition);
            Assert.That(result, Is.EqualTo(expectedPosition).Within(0.1));
        }

        [Test]
        [TestCase(15.0f, 15.0f, Description = "LITERAL range, exact mechanical movement")]
        [TestCase(90.9f, 90.9f, Description = "LITERAL range, exact mechanical movement")]
        [TestCase(195.0f, 195.0f, Description = "LITERAL range, no range mapping")]
        [TestCase(359.5f, 359.5f, Description = "LITERAL range, near 360°")]
        public async Task Test_MoveMechanical_LiteralRange(float requestedPosition, float expectedPosition) {
            var sut = await CreateSUT();
            rangeType = RotatorRangeTypeEnum.LITERAL;
            rangeStartMechanicalPosition = 0.0f;
            isSynced = true;
            mechanicalPosition = 10.0f;
            offset = 5.0f;

            var cts = new CancellationTokenSource();
            var result = await sut.MoveMechanical(requestedPosition, TimeSpan.Zero, cts.Token);
            Assert.That(result, Is.EqualTo(expectedPosition).Within(0.1));
            mockRotator.Verify(x => x.MoveAbsoluteMechanical(expectedPosition, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(15.0f, 15.0f, 10.0f, Description = "FULL range, direct movement")]
        [TestCase(195.0f, 15.0f, 10.0f, Description = "FULL range, reciprocal optimization (195° becomes 15° from 10°)")]
        [TestCase(100.0f, 100.0f, 90.0f, Description = "FULL range, close positions")]
        [TestCase(280.0f, 100.0f, 90.0f, Description = "FULL range, reciprocal optimization (280° becomes 100° from 90°)")]
        public async Task Test_MoveMechanical_FullRange(float requestedPosition, float expectedPosition, float currentMechanicalPosition) {
            var sut = await CreateSUT();
            rangeType = RotatorRangeTypeEnum.FULL;
            rangeStartMechanicalPosition = 0.0f;
            isSynced = true;
            mechanicalPosition = currentMechanicalPosition;
            offset = 5.0f;

            // Update RotatorInfo to reflect the current mechanical position
            sut.RotatorInfo.MechanicalPosition = currentMechanicalPosition;

            var cts = new CancellationTokenSource();
            var result = await sut.MoveMechanical(requestedPosition, TimeSpan.Zero, cts.Token);
            Assert.That(result, Is.EqualTo(expectedPosition).Within(0.1));
            mockRotator.Verify(x => x.MoveAbsoluteMechanical(expectedPosition, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}