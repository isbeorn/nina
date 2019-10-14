using System;
using System.Threading.Tasks;
using NINA.Model.MyFlatDevice;
using NUnit.Framework;

namespace NINATest {

    [TestFixture]
    public class AlnitakFlatDeviceTest {

        [Test]
        public async Task TestCoverStatus() {
            AlnitakFlatDevice sut = new AlnitakFlatDevice("COM3");
            _ = sut.MaxBrightness;
            _ = sut.MinBrightness;
            _ = await sut.Connect(new System.Threading.CancellationToken());
            _ = sut.Description;
            _ = sut.CoverState;
            _ = sut.Brightness;
            _ = sut.LightOn;
            _ = sut.LightOn = true;
            _ = sut.LightOn;
            _ = await sut.Open(new System.Threading.CancellationToken());
            _ = await sut.Close(new System.Threading.CancellationToken());
            //            sut.Disconnect();
        }
    }
}