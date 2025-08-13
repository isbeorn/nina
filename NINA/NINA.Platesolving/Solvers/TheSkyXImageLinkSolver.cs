#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Image.FileFormat;
using NINA.Image.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving.Solvers {
    internal class TheSkyXImageLinkSolver : BaseSolver {

        private string _tsxHost;
        private int _tsxPort;

        public TheSkyXImageLinkSolver(string tsxHost, int tsxPort) {
            this._tsxHost = tsxHost;
            this._tsxPort = tsxPort;
        }

        protected override async Task<PlateSolveResult> SolveAsyncImpl(IImageData source, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties, IProgress<ApplicationStatus> progress, CancellationToken cancelToken) {

            PlateSolveResult result = new PlateSolveResult() { Success = false };

            try {
                // Make sure we are not canceled already.
                cancelToken.ThrowIfCancellationRequested();

                // Save off image that TheSkyX can access
                progress?.Report(new ApplicationStatus() { Status = "Preparing image for TheSkyX ImageLink..." });
                var imagePath = await PrepareAndSaveImage(source, cancelToken);

                try {
                    var imageLink = new ImageLink(new DnsEndPoint(this._tsxHost, this._tsxPort, System.Net.Sockets.AddressFamily.InterNetwork));

                    // Execute the TSX script
                    progress?.Report(new ApplicationStatus() { Status = "Solving image with TheSkyX Imagelink..." });
                    imageLink.Execute(imagePath.Replace(@"\", "/"), imageScale: imageProperties.ArcSecPerPixel, isUnknownScale: true);

                    // Get the results of the last ImageLInk
                    progress?.Report(new ApplicationStatus() { Status = $"Retrieving results for requested image..." });
                    var imageLinkResults = imageLink.GetLastImageLinkResults();

                    if ((imageLinkResults != null) && (imageLinkResults.Succeeded)) {
                        result.Success = true;

                        result.Flipped = imageLinkResults.IsImageMirrored;
                        result.PositionAngle = imageLinkResults.ImagePositionAngle;

                        result.Pixscale = imageLinkResults.ImageScale;
                        if (!double.IsNaN(result.Pixscale)) {
                            result.Radius = AstroUtil.ArcsecToDegree(Math.Sqrt(Math.Pow(imageProperties.ImageWidth * result.Pixscale, 2) + Math.Pow(imageProperties.ImageHeight * result.Pixscale, 2)) / 2d);
                        }
                        result.Coordinates = new Astrometry.Coordinates(imageLinkResults.ImageCenterRAJ2000, imageLinkResults.ImageCenterDecJ2000, Astrometry.Epoch.J2000, Astrometry.Coordinates.RAType.Hours);

                        progress?.Report(new ApplicationStatus() { Status = "Plate solve completed." });
                    } else {
                        result.Success = false;
                        progress?.Report(new ApplicationStatus() { Status = $"Plate solve failed. {imageLinkResults?.ErrorText}" });
                    }
                } catch (Exception) {
                    result.Success = false;
                    throw;
                } finally {
                    var filePrefix = FAILED_FILENAME;
                    if (!string.IsNullOrWhiteSpace(source?.MetaData?.Target?.Name)) {
                        filePrefix += $".{CoreUtil.ReplaceAllInvalidFilenameChars(source.MetaData.Target.Name)}";
                    }

                    if (parameter.Coordinates == null) {
                        filePrefix += ".blind";
                    }

                    if (imagePath != null && File.Exists(imagePath)) {
                        MoveOrDeleteFile(result, imagePath, filePrefix, cancelToken);
                    }
                }
            } catch (OperationCanceledException) {
                result.Success = false;
            } catch (Exception ex) {
                result.Success = false;

                //var errorMessage = $"Error plate solving with TheSkyX Imagelink. {ex.Message}";
                progress?.Report(new ApplicationStatus() { Status = ex.Message });
                if (!parameter.DisableNotifications) {
                    Notification.ShowError(ex.Message);
                }
            }

            return result;
        }
       

        private void MoveOrDeleteFile(PlateSolveResult result, string file, string movedFilePrefix, CancellationToken cancelToken) {
            try {
                if (!result.Success && !cancelToken.IsCancellationRequested) {
                    if (File.Exists(file)) {
                        var destination = Path.Combine(FAILED_DIRECTORY, $"{movedFilePrefix}.{Path.GetExtension(file)}");
                        if (File.Exists(destination)) {
                            File.Delete(destination);
                        }
                        File.Move(file, destination);
                    }
                } else {
                    File.Delete(file);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        protected async Task<string> PrepareAndSaveImage(IImageData source, CancellationToken cancelToken) {
            FileSaveInfo fileSaveInfo = new FileSaveInfo {
                FilePath = WORKING_DIRECTORY,
                FilePattern = Path.GetRandomFileName(),
                FileType = FileTypeEnum.FITS
            };

            return await source.SaveToDisk(fileSaveInfo, cancelToken, forceFileType: true);
        }

    }

    internal sealed class ImageLink {
        internal ImageLink(EndPoint endpoint) 
        {
            this.EndPoint = endpoint;
        }

        public EndPoint EndPoint {
            get; private set;
        }

        public void Execute(string pathToFITS, double imageScale = 2.00, bool isUnknownScale = true) {
            var script = new StringBuilder();
            script.AppendLine($"ImageLink.scale = {imageScale};");
            script.AppendLine($"ImageLink.unknownScale = {(isUnknownScale ? 1 : 0)};");

            script.AppendLine($"ImageLink.pathToFITS = '{pathToFITS?.Trim()}';");
            script.AppendLine("ImageLink.execute();");

            this.SendToTheSkyX(script.ToString(), out var errorMessage);
        }

        public void Execute() {
            this.SendToTheSkyX("Imagelink.execute()", out var errorMessage);
        }

        public string PathToFITS {
            get {
                var model = this.SendToTheSkyX("ImageLink.pathToFITS", out var errorMessage);

                if (string.IsNullOrWhiteSpace(model)) {
                    throw new InvalidDataException("No response received.");
                }

                return model;
            }
            set {
                this.SendToTheSkyX($"ImageLink.pathToFITS = '{value?.Trim()}';", out var errorMessage);
            }
        }

        public double Scale {
            get {
                var model = this.SendToTheSkyX("ImageLink.scale", out var errorMessage);

                if (string.IsNullOrWhiteSpace(model)) {
                    throw new InvalidDataException("No response received.");
                }

                double.TryParse(model, CultureInfo.InvariantCulture, out var result);
                return result;
            }
            set {
                this.SendToTheSkyX($"ImageLink.scale = {value};", out var errorMessage);
            }
        }

        public bool IsUnknownScale {
            get {
                var model = this.SendToTheSkyX("ImageLink.unknownScale", out var errorMessage);

                if (string.IsNullOrWhiteSpace(model)) {
                    throw new InvalidDataException("No response received.");
                }

                int.TryParse(model, out var result);
                return result == 1;
            }
            set {
                this.SendToTheSkyX($"ImageLink.unknownScale = {(value ? 1 : 0)};", out var errorMessage);
            }
        }

        public bool IsImageLinkSuccess {
            get {
                var model = this.SendToTheSkyX("ImageLinkResults.succeeded", out var errorMessage);

                if (string.IsNullOrWhiteSpace(model)) {
                    throw new InvalidDataException("No response received.");
                }

                int.TryParse(model, out var result);
                return result == 1;
            }
        }

        public int LastImageLinkErrorCode {
            get {
                var model = this.SendToTheSkyX("ImageLinkResults.errorCode", out var errorMessage);

                if (string.IsNullOrWhiteSpace(model)) {
                    throw new InvalidDataException("No response received.");
                }

                int.TryParse(model, out var result);
                return result;
            }
        }

        public ImageLinkResults GetLastImageLinkResults() {
            var sb = new StringBuilder();
            sb.AppendLine("var result;");
            sb.AppendLine("var objResult = {");
            sb.AppendLine("  errorCode : ImageLinkResults.errorCode,");
            sb.AppendLine("  succeeded: ImageLinkResults.succeeded == 1,");
            sb.AppendLine("  searchAborted: ImageLinkResults.searchAborted == 1,");
            sb.AppendLine("  errorText: ImageLinkResults.errorText,");
            sb.AppendLine("  imageScale: ImageLinkResults.imageScale,");
            sb.AppendLine("  imagePositionAngle: ImageLinkResults.imagePositionAngle,");
            sb.AppendLine("  imageCenterRAJ2000: ImageLinkResults.imageCenterRAJ2000,");
            sb.AppendLine("  imageCenterDecJ2000: ImageLinkResults.imageCenterDecJ2000,");
            sb.AppendLine("  imageSize: { width: ImageLinkResults.imageWidthInPixels, height: ImageLinkResults.imageHeightInPixels },");
            sb.AppendLine("  imageIsMirrored: ImageLinkResults.imageIsMirrored == 1,");
            sb.AppendLine("  imageFilePath: ImageLinkResults.imageFilePath,");
            sb.AppendLine("  imageStarCount: ImageLinkResults.imageStarCount,");
            sb.AppendLine("  imageFWHMInArcSeconds: ImageLinkResults.imageFWHMInArcSeconds,");
            sb.AppendLine("  solutionRMS: ImageLinkResults.solutionRMS,");
            sb.AppendLine("  solutionRMSX: ImageLinkResults.solutionRMSX,");
            sb.AppendLine("  solutionRMSY: ImageLinkResults.solutionRMSY,");
            sb.AppendLine("  solutionStarCount: ImageLinkResults.solutionStarCount,");
            sb.AppendLine("  catalogStarCount: ImageLinkResults.catalogStarCount");
            sb.AppendLine("};");

            sb.AppendLine("result = JSON.stringify(objResult);");

            var result = this.SendToTheSkyX(sb.ToString(), 2048, out var errorMessage);

            var model = JsonSerializer.Deserialize<ImageLinkResults>(result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            return model;
        }


        internal string SendToTheSkyX(string scriptBlock, int maxResultLength, out string errorMessage) {
            errorMessage = string.Empty;
            var resultText = string.Empty;

            // Our message template
            string messageText = $"/* Java Script */{Environment.NewLine}/* Socket Start Packet */{Environment.NewLine}{Environment.NewLine}{scriptBlock}{Environment.NewLine}{Environment.NewLine}/* Socket End Packet */";

            // Encode the message to send
            var messageBytes = Encoding.UTF8.GetBytes(messageText);

            // Create the socket, send the data and receive the result.
            using (var theSkyXSocket = new Socket(this.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)) {
                try {
                    theSkyXSocket.Connect(this.EndPoint);
                } catch (SocketException) {
                    // This is just debug code for right now.
                    throw;
                }

                // Send the message
                var bytesSent = theSkyXSocket.Send(messageBytes, 0, (messageBytes != null) ? messageBytes.Length : 0, SocketFlags.None, out var socketError);
                if ((socketError == SocketError.Success) && (bytesSent > 0)) {
                    var receivedBytes = new byte[maxResultLength];
                    var bytesReceived = theSkyXSocket.Receive(receivedBytes, 0, (receivedBytes != null) ? receivedBytes.Length : 0, SocketFlags.None, out socketError);
                    if ((socketError == SocketError.Success) && (bytesReceived > 0)) {
                        resultText = Encoding.UTF8.GetString(receivedBytes, 0, bytesReceived);
                    } else {
                        errorMessage = socketError.ToString();
                        return string.Empty;
                    }
                } else {
                    errorMessage = socketError.ToString();
                    return string.Empty;
                }
            }

            if (resultText.StartsWith("{\"lst\":") == false) {
                Console.WriteLine(resultText);
            }

            // Split out the status message from the return value.
            if (string.IsNullOrWhiteSpace(resultText) == false) {
                var errorMessageIndex = resultText.LastIndexOf('|');
                if (errorMessageIndex != -1) {
                    errorMessage = resultText.Substring(errorMessageIndex + 1); // Make sure to remove the '|' character.
                    resultText = resultText.Substring(0, errorMessageIndex);
                }

                // The errorMessage that TSX appends seems to be meaningless.  If the script errors out, it does not use that, but returns the error string and then sets the statusMessage to "No Error".  
                // Kinda stupid.  We will work around that below when we find issues.
                if (resultText.StartsWith("TypeError: ") || errorMessage.StartsWith("TypeError: ")) {
                    if (resultText.StartsWith("TypeError: ")) {
                        errorMessage = resultText;
                    }

                    resultText = string.Empty;

                    Regex r = new Regex(@"^TypeError:\s*(?'errorMessage'.*)Error\s*=\s*(?'errorCode'[0-9]+)");
                    var match = r.Match(errorMessage);

                    if (match.Groups.Count > 2) {
                        errorMessage = match.Groups[1]?.Value?.Trim();
                        int.TryParse(match.Groups[2]?.Value?.Trim(), out var errorCode);

                        throw new TheSkyXException(errorMessage, errorCode);
                    }
                } else if (resultText.StartsWith("ParseError")) {
                    errorMessage = resultText;
                    resultText = string.Empty;
                }
            }

            return resultText;
        }

        internal string SendToTheSkyX(string scriptBlock, out string errorMessage) {
            // The default size for script results is 256 bytes .. plenty for most things. 
            return SendToTheSkyX(scriptBlock, 256, out errorMessage);
        }


        public sealed class ImageLinkResults {
            public int ErrorCode { get; set; }

            public bool Succeeded { get; set; }

            public bool SearchAborted { get; set; }

            public string ErrorText { get; set; }

            public double ImageScale { get; set; }

            public double ImagePositionAngle { get; set; }

            public double ImageCenterRAJ2000 { get; set; }

            public double ImageCenterDecJ2000 { get; set; }

            public Size ImageSize { get; set; }

            public bool IsImageMirrored { get; set; }

            public string ImageFilePath { get; set; }

            public int ImageStarCount { get; set; }

            public double ImageFWHMInArcSeconds { get; set; }

            public double SolutionRMS { get; set; }

            public double SolutionRMSX { get; set; }

            public double SolutionRMSY { get; set; }

            public int SolutionStarCount { get; set; }

            public int CatalogStarCount { get; set; }
        }

        [Serializable]
        public class TheSkyXException : Exception {
            public TheSkyXException(string message) : base(message) {
            }

            public TheSkyXException(string message, int errorCode) : this(message) {
                this.ErrorCode = errorCode;
            }

            public int ErrorCode {
                get; private set;
            } = -1;

            public TheSkyXException(string message, Exception innerException) : base(message, innerException) {
            }

            protected TheSkyXException(SerializationInfo info, StreamingContext context) : base(info, context) {
            }
        }
    }
}
