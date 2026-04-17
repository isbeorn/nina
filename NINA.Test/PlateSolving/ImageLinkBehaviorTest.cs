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
using NINA.PlateSolving.Solvers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class ImageLinkBehaviorTest {

        /// <summary>
        /// Verifies TheSkyX socket responses are split into result and status text while the request is wrapped in the expected JavaScript packet.
        /// </summary>
        [Test]
        public async Task SendToTheSkyX_ReturnsResultAndExtractsTrailingStatusMessage() {
            ((string result, string errorMessage), string request, _) = await RunExchange(
                "42|No Error",
                imageLink => {
                    string result = imageLink.SendToTheSkyX("ImageLink.scale", out string errorMessage);
                    return (result, errorMessage);
                });

            result.Should().Be("42");
            errorMessage.Should().Be("No Error");
            request.Should().Contain("/* Socket Start Packet */");
            request.Should().Contain("ImageLink.scale");
            request.Should().Contain("/* Socket End Packet */");
        }

        /// <summary>
        /// Verifies TypeError responses from TheSkyX are promoted to typed exceptions with the parsed ImageLink error code.
        /// </summary>
        [Test]
        public async Task SendToTheSkyX_TypeErrorResponseThrowsTheSkyXExceptionWithCode() {
            Func<Task> act = async () => await RunExchange<string>(
                "TypeError: Catalog not available Error = 321|No Error",
                imageLink => imageLink.SendToTheSkyX("ImageLink.execute();", out _));

            await act.Should().ThrowAsync<ImageLink.TheSkyXException>()
                .Where(ex => ex.ErrorCode == 321)
                .WithMessage("*Catalog not available*");
        }

        /// <summary>
        /// Verifies ImageLink property getters parse scalar TheSkyX responses into invariant .NET types.
        /// </summary>
        [Test]
        public async Task Properties_ParseScalarResponses() {
            (string pathToFits, _, _) = await RunExchange("C:/Images/light.fit|No Error", imageLink => imageLink.PathToFITS);
            (double scale, _, _) = await RunExchange("1.25|No Error", imageLink => imageLink.Scale);
            (bool unknownScale, _, _) = await RunExchange("1|No Error", imageLink => imageLink.IsUnknownScale);
            (bool success, _, _) = await RunExchange("0|No Error", imageLink => imageLink.IsImageLinkSuccess);
            (int errorCode, _, _) = await RunExchange("17|No Error", imageLink => imageLink.LastImageLinkErrorCode);

            pathToFits.Should().Be("C:/Images/light.fit");
            scale.Should().BeApproximately(1.25, 1e-10);
            unknownScale.Should().BeTrue();
            success.Should().BeFalse();
            errorCode.Should().Be(17);
        }

        /// <summary>
        /// Verifies ImageLink property setters and execute calls emit the expected TheSkyX JavaScript assignments.
        /// </summary>
        [Test]
        public async Task Mutators_SendExpectedJavaScriptAssignments() {
            (_, string pathRequest, _) = await RunExchange("0|No Error", imageLink => {
                imageLink.PathToFITS = " C:/Images/light.fit ";
                return 0;
            });
            (_, string scaleRequest, _) = await RunExchange("0|No Error", imageLink => {
                imageLink.Scale = 2.5;
                return 0;
            });
            (_, string unknownScaleRequest, _) = await RunExchange("0|No Error", imageLink => {
                imageLink.IsUnknownScale = false;
                return 0;
            });
            (_, string executeRequest, _) = await RunExchange("0|No Error", imageLink => {
                imageLink.Execute(" C:/Images/light.fit ", imageScale: 1.75, isUnknownScale: false);
                return 0;
            });

            pathRequest.Should().Contain("ImageLink.pathToFITS = 'C:/Images/light.fit';");
            scaleRequest.Should().Contain("ImageLink.scale = 2.5;");
            unknownScaleRequest.Should().Contain("ImageLink.unknownScale = 0;");
            executeRequest.Should().Contain("ImageLink.scale = 1.75;");
            executeRequest.Should().Contain("ImageLink.unknownScale = 0;");
            executeRequest.Should().Contain("ImageLink.pathToFITS = 'C:/Images/light.fit';");
            executeRequest.Should().Contain("ImageLink.execute();");
        }

        /// <summary>
        /// Verifies full ImageLink result JSON is deserialized case-insensitively into the typed result model used by the solver.
        /// </summary>
        [Test]
        public async Task GetLastImageLinkResults_ParsesResultJson() {
            const string response = """
                {"errorCode":0,"succeeded":true,"searchAborted":false,"errorText":"","imageScale":0.97,"imagePositionAngle":123.4,"imageCenterRAJ2000":12.5,"imageCenterDecJ2000":-22.25,"imageSize":{"width":300,"height":200},"imageIsMirrored":true,"imageFilePath":"C:/Images/light.fit","imageStarCount":42,"imageFWHMInArcSeconds":3.1,"solutionRMS":0.4,"solutionRMSX":0.2,"solutionRMSY":0.3,"solutionStarCount":30,"catalogStarCount":120}|No Error
                """;

            (ImageLink.ImageLinkResults result, string request, _) = await RunExchange(response.Trim(), imageLink => imageLink.GetLastImageLinkResults());

            result.Succeeded.Should().BeTrue();
            result.ImageScale.Should().BeApproximately(0.97, 1e-10);
            result.ImagePositionAngle.Should().BeApproximately(123.4, 1e-10);
            result.ImageCenterRAJ2000.Should().BeApproximately(12.5, 1e-10);
            result.ImageCenterDecJ2000.Should().BeApproximately(-22.25, 1e-10);
            result.ImageSize.Width.Should().Be(300);
            result.ImageSize.Height.Should().Be(200);
            result.IsImageMirrored.Should().BeTrue();
            result.ImageStarCount.Should().Be(42);
            result.CatalogStarCount.Should().Be(120);
            request.Should().Contain("JSON.stringify(objResult)");
            request.Should().Contain("ImageLinkResults.imageScale");
        }

        private static async Task<(T Result, string Request, string ErrorMessage)> RunExchange<T>(string response, Func<ImageLink, T> action) {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var endpoint = new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
            Task<string> server = Task.Run(async () => {
                using TcpClient client = await listener.AcceptTcpClientAsync();
                await using NetworkStream stream = client.GetStream();
                var buffer = new byte[8192];
                int bytesRead = await stream.ReadAsync(buffer);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes);
                return request;
            });

            var imageLink = new ImageLink(endpoint);
            T result = action(imageLink);
            string request = await server.WaitAsync(TimeSpan.FromSeconds(5));
            return (result, request, ExtractErrorMessage(response));
        }

        private static string ExtractErrorMessage(string response) {
            int separator = response.LastIndexOf('|');
            return separator < 0 ? string.Empty : response[(separator + 1)..];
        }
    }
}
