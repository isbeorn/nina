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
            Console.WriteLine(sut.Description);
            _ = await sut.Open(new System.Threading.CancellationToken());
            _ = await sut.Close(new System.Threading.CancellationToken());
            Console.WriteLine(sut.CoverState);
            Console.WriteLine(sut.LightOn);
            Console.WriteLine(sut.Brightness);
            sut.LightOn = true;
            Console.WriteLine(sut.Brightness);
            sut.Brightness = 200;
            Console.WriteLine(sut.Brightness);
            sut.Brightness = sut.MinBrightness;
            Console.WriteLine(sut.Brightness);
            sut.Brightness = sut.MaxBrightness;
            Console.WriteLine(sut.Brightness);
            Console.WriteLine(sut.LightOn);
            Console.WriteLine(sut.LightOn);
            sut.LightOn = false;
            //            sut.Disconnect();
        }
    }
}