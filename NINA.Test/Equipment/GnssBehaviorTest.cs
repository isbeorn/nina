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
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyGPS;
using NINA.Equipment.Equipment.MyGPS.PegasusAstro;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class GnssBehaviorTest {

        /// <summary>
        /// Verifies GPSD reads WATCH-enabled TPV messages and returns a 3D fix with latitude, longitude, and MSL altitude.
        /// </summary>
        [Test]
        public async Task Gpsd_GetLocationReturnsThreeDimensionalFixFromTpvMessage() {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            ushort port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
            Task<string> server = ServeGpsdLines(listener,
                """{"class":"VERSION","mode":3}""",
                """{"class":"TPV","mode":3,"lat":48.137154,"lon":11.576124,"altMSL":521.4}""");
            var sut = new Gpsd(CreateProfile("127.0.0.1", port).Object);

            Location location = await sut.GetLocation(CancellationToken.None);
            string watchCommand = await server.WaitAsync(TimeSpan.FromSeconds(5));

            location.Latitude.Should().BeApproximately(48.137154, 1e-10);
            location.Longitude.Should().BeApproximately(11.576124, 1e-10);
            location.Elevation.Should().BeApproximately(521.4, 1e-10);
            watchCommand.Should().Contain("?WATCH=");
            watchCommand.Should().Contain("\"json\":true");
        }

        /// <summary>
        /// Verifies GPSD reports no-fix conditions distinctly from connection failures when the receiver only has a 2D fix.
        /// </summary>
        [Test]
        public async Task Gpsd_GetLocationThrowsNoFixForTwoDimensionalTpvMessage() {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            ushort port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
            Task<string> server = ServeGpsdLines(listener, """{"class":"TPV","mode":2,"lat":48.1,"lon":11.5}""");
            var sut = new Gpsd(CreateProfile("127.0.0.1", port).Object);

            Func<Task> act = () => sut.GetLocation(CancellationToken.None);

            await act.Should().ThrowAsync<GnssNoFixException>();
            _ = await server.WaitAsync(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Verifies GPSD validates host configuration before attempting a socket connection.
        /// </summary>
        [Test]
        public async Task Gpsd_GetLocationRejectsMissingHostBeforeNetworkConnection() {
            var sut = new Gpsd(CreateProfile("", 2947).Object);

            Func<Task> act = () => sut.GetLocation(CancellationToken.None);

            await act.Should().ThrowAsync<GnssInvalidHostException>();
        }

        /// <summary>
        /// Verifies TPV JSON maps the GPSD wire field names to NINA's strongly typed message model and diagnostic string.
        /// </summary>
        [Test]
        public void TpvMessage_DeserializesGpsdWireNames() {
            const string json = """{"class":"TPV","mode":3,"lat":12.5,"lon":-45.25,"altMSL":1234.5}""";

            Gpsd.TpvMessage message = JsonConvert.DeserializeObject<Gpsd.TpvMessage>(json);

            message.Class.Should().Be("TPV");
            message.Mode.Should().Be(3);
            message.Latitude.Should().BeApproximately(12.5, 1e-10);
            message.Longitude.Should().BeApproximately(-45.25, 1e-10);
            message.Altitude.Should().BeApproximately(1234.5, 1e-10);
            message.ToString().Should().Contain("Latitude: 12.5").And.Contain("Altitude: 1234.5");
        }

        /// <summary>
        /// Verifies the GNSS factory maps every configured source enum to the expected source implementation.
        /// </summary>
        [Test]
        public void GnssFactory_MapsConfiguredSourcesToImplementations() {
            var profile = CreateProfile("localhost", 2947);
            var factory = new GnssFactory(profile.Object);

            factory.GetGnssSource(GnssSourceEnum.NmeaSerial).Should().BeOfType<NMEAGps>();
            factory.GetGnssSource(GnssSourceEnum.Gpsd).Should().BeOfType<Gpsd>();
            factory.GetGnssSource(GnssSourceEnum.PrimaLuceLabEagle).Should().BeOfType<PrimaLuceLabEagle>();
            factory.GetGnssSource(GnssSourceEnum.PegausAstroUranusMeteo).Should().BeOfType<UranusMeteo>();
            factory.GetGnssSource((GnssSourceEnum)999).Should().BeNull();
        }

        private static Mock<IProfileService> CreateProfile(string host, ushort port) {
            var profile = new Mock<IProfileService>();
            profile.SetupGet(x => x.ActiveProfile.GnssSettings.GpsdHost).Returns(host);
            profile.SetupGet(x => x.ActiveProfile.GnssSettings.GpsdPort).Returns(port);
            profile.SetupGet(x => x.ActiveProfile.GnssSettings.GnssSource).Returns(GnssSourceEnum.Gpsd);
            return profile;
        }

        private static async Task<string> ServeGpsdLines(TcpListener listener, params string[] lines) {
            using TcpClient client = await listener.AcceptTcpClientAsync();
            await using NetworkStream stream = client.GetStream();
            var buffer = new byte[512];
            int bytesRead = await stream.ReadAsync(buffer);
            string watchCommand = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            foreach (string line in lines) {
                byte[] bytes = Encoding.ASCII.GetBytes(line + "\n");
                await stream.WriteAsync(bytes);
                await stream.FlushAsync();
            }

            return watchCommand;
        }
    }
}
