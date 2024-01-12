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
using Castle.DynamicProxy;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NINA.Core.API.ASCOM.Camera;
using NINA.Core.Utility;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static NINA.Equipment.Model.CaptureSequence;

namespace NINA.Equipment.Equipment.MyCamera {

    internal class GrpcErrorPropagatingProxy<T> : IAsyncInterceptor where T : class {
        private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();
        private static readonly Newtonsoft.Json.JsonSerializer jsonSerializer = Newtonsoft.Json.JsonSerializer.Create();

        public GrpcErrorPropagatingProxy() {
        }

        public static T Wrap(T wrapped) {
            if (typeof(T).IsInterface) {
                return proxyGenerator.CreateInterfaceProxyWithTarget(wrapped, new GrpcErrorPropagatingProxy<T>());
            } else {
                return proxyGenerator.CreateClassProxyWithTarget(wrapped, new GrpcErrorPropagatingProxy<T>());
            }
        }

        public void InterceptAsynchronous(IInvocation invocation) {
            invocation.Proceed();
            Task result = (Task)invocation.ReturnValue;
            if (result.IsFaulted) {
                var translatedException = TranslateException(result.Exception);
                if (!Object.ReferenceEquals(translatedException, result.Exception)) {
                    invocation.ReturnValue = Task.FromException(translatedException);
                }
            }
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation) {
            invocation.Proceed();
            Task<TResult> result = (Task<TResult>)invocation.ReturnValue;
            if (result.IsFaulted) {
                var translatedException = TranslateException(result.Exception);
                if (!Object.ReferenceEquals(translatedException, result.Exception)) {
                    invocation.ReturnValue = Task.FromException<TResult>(translatedException);
                }
            }
        }

        public void InterceptSynchronous(IInvocation invocation) {
            try {
                invocation.Proceed();
            } catch (Exception e) {
                var translatedException = TranslateException(e);
                if (Object.ReferenceEquals(e, translatedException)) {
                    throw;
                } else {
                    throw translatedException;
                }
            }
        }

        private Exception TranslateException(Exception e) {
            var innerException = e;
            if (e is AggregateException) {
                innerException = ((AggregateException)e).InnerException;
            }

            if (innerException is RpcException) {
                return innerException;
            } else {
                // Serialize the exception so the client can receive it
                return new RpcException(new Status(StatusCode.Internal, SerializeException(innerException)));
            }
        }

        private string SerializeException(Exception e) {
            using (var sw = new StringWriter()) {
                jsonSerializer.Serialize(sw, e);
                return sw.ToString();
            }
        }
    }

    internal class SBIGServerLoggingProxy<T> : IAsyncInterceptor where T : class {
        private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

        private readonly SBIGCamera camera;

        public SBIGServerLoggingProxy(SBIGCamera camera) {
            this.camera = camera;
        }

        public static T Wrap(T wrapped, SBIGCamera camera) {
            if (typeof(T).IsInterface) {
                return proxyGenerator.CreateInterfaceProxyWithTarget(wrapped, new SBIGServerLoggingProxy<T>(camera));
            } else {
                return proxyGenerator.CreateClassProxyWithTarget(wrapped, new SBIGServerLoggingProxy<T>(camera));
            }
        }

        public void InterceptAsynchronous(IInvocation invocation) {
            if (camera.TrackingCcdAscomServerLoggingEnabled) {
                Logger.Debug($"SBIG: ASCOM Server method {invocation.Method.Name} started");
            }

            invocation.Proceed();
            Task result = (Task)invocation.ReturnValue;
            if (result.IsFaulted) {
                if (camera.TrackingCcdAscomServerLoggingEnabled) {
                    Logger.Error($"SBIG: ASCOM Server method {invocation.Method.Name} failed", result.Exception);
                }
            } else {
                if (camera.TrackingCcdAscomServerLoggingEnabled) {
                    Logger.Debug($"SBIG: ASCOM Server method {invocation.Method.Name} completed");
                }
            }
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation) {
            if (camera.TrackingCcdAscomServerLoggingEnabled) {
                Logger.Debug($"SBIG: ASCOM Server method {invocation.Method.Name} started");
            }

            invocation.Proceed();
            Task<TResult> result = (Task<TResult>)invocation.ReturnValue;
            if (result.IsFaulted) {
                if (camera.TrackingCcdAscomServerLoggingEnabled) {
                    Logger.Error($"SBIG: ASCOM Server method {invocation.Method.Name} failed", result.Exception);
                }
            } else {
                if (camera.TrackingCcdAscomServerLoggingEnabled) {
                    Logger.Debug($"SBIG: ASCOM Server method {invocation.Method.Name} completed");
                }
            }
        }

        public void InterceptSynchronous(IInvocation invocation) {
            if (camera.TrackingCcdAscomServerLoggingEnabled) {
                Logger.Debug($"SBIG: ASCOM Server method {invocation.Method.Name} started");
            }

            try {
                invocation.Proceed();
                if (camera.TrackingCcdAscomServerLoggingEnabled) {
                    Logger.Debug($"SBIG: ASCOM Server method {invocation.Method.Name} completed");
                }
            } catch (Exception e) {
                if (camera.TrackingCcdAscomServerLoggingEnabled) {
                    Logger.Error($"SBIG: ASCOM Server method {invocation.Method.Name} failed", e);
                }
                throw;
            }
        }
    }

    public class SBIGCameraASCOMService : CameraService.CameraServiceBase {
        private readonly SBIGCamera camera;
        private DateTime? latestExposureStart;
        private double? latestExposureDuration;
        private CancellationTokenSource exposeAndDownloadCts;
        private Task<IImageData> exposeAndDownloadTask;

        public SBIGCameraASCOMService(SBIGCamera camera) {
            this.camera = camera;
        }

        public override async Task<Empty> AbortExposure(
            Empty request,
            ServerCallContext context) {
            this.camera.AbortExposure();
            try { this.exposeAndDownloadCts?.Cancel(); } catch { }
            this.exposeAndDownloadCts = new CancellationTokenSource();
            return new Empty();
        }

        public override async Task<Empty> StartExposure(
            StartExposureRequest request,
            ServerCallContext context) {
            if (this.camera.SubSampleX > 0 || this.camera.SubSampleY > 0 || this.camera.SubSampleHeight < (this.camera.CameraYSize / this.camera.BinY) || this.camera.SubSampleWidth < (this.camera.CameraXSize / this.camera.BinX)) {
                this.camera.EnableSubSample = true;
            } else {
                this.camera.EnableSubSample = false;
            }

            var captureParams = new CaptureSequence() {
                ExposureTime = request.Duration,
                ImageType = request.Light ? ImageTypes.LIGHT : ImageTypes.DARK
            };

            this.latestExposureDuration = request.Duration;
            this.latestExposureStart = DateTime.Now;
            try { this.exposeAndDownloadCts?.Cancel(); } catch { }
            this.exposeAndDownloadCts = new CancellationTokenSource();
            var ct = this.exposeAndDownloadCts.Token;
            this.camera.StartExposure(captureParams);
            this.exposeAndDownloadTask = Task.Run(async () => {
                await this.camera.WaitUntilExposureIsReady(ct);
                ct.ThrowIfCancellationRequested();

                var exposureData = await this.camera.DownloadExposure(ct);
                ct.ThrowIfCancellationRequested();
                if (exposureData != null) {
                    return await exposureData.ToImageData();
                }
                return null;
            }, ct);
            return new Empty();
        }

        public override async Task<Empty> StopExposure(
            Empty request,
            ServerCallContext context) {
            this.camera.StopExposure();
            try { this.exposeAndDownloadCts?.Cancel(); this.exposeAndDownloadCts?.Dispose(); } catch { }
            this.exposeAndDownloadCts = null;
            return new Empty();
        }

        public override async Task<GetShortPropertyReply> BayerOffsetX_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.BayerOffsetX };
        }

        public override async Task<GetShortPropertyReply> BayerOffsetY_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.BayerOffsetY };
        }

        public override async Task<GetShortPropertyReply> BinX_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.BinX };
        }

        public override async Task<Empty> BinX_set(
            SetShortPropertyRequest request,
            ServerCallContext context) {
            this.camera.BinX = checked((short)request.Value);
            return new Empty();
        }

        public override async Task<GetShortPropertyReply> BinY_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.BinY };
        }

        public override async Task<Empty> BinY_set(
            SetShortPropertyRequest request,
            ServerCallContext context) {
            this.camera.BinY = checked((short)request.Value);
            return new Empty();
        }

        public override async Task<GetCameraStatesReply> CameraState_get(
            Empty request,
            ServerCallContext context) {
            CameraStates cameraState;
            switch (this.camera.CameraStatus) {
                case SBIGCamera.SBIGCameraStatus.IDLE:
                    cameraState = CameraStates.CameraIdle;
                    break;

                case SBIGCamera.SBIGCameraStatus.DOWNLOAD:
                    cameraState = CameraStates.CameraDownload;
                    break;

                case SBIGCamera.SBIGCameraStatus.ERROR:
                    cameraState = CameraStates.CameraError;
                    break;

                case SBIGCamera.SBIGCameraStatus.EXPOSING:
                    cameraState = CameraStates.CameraExposing;
                    break;

                case SBIGCamera.SBIGCameraStatus.WAITING:
                    cameraState = CameraStates.CameraWaiting;
                    break;

                default:
                    throw new ArgumentException($"{this.camera.CameraStatus} not an expected value");
            }

            return new GetCameraStatesReply() { Value = cameraState };
        }

        public override async Task<GetIntPropertyReply> CameraXSize_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = this.camera.CameraXSize / this.camera.BinX };
        }

        public override async Task<GetIntPropertyReply> CameraYSize_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = this.camera.CameraYSize / this.camera.BinY };
        }

        public override async Task<GetBoolPropertyReply> CanAbortExposure_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = true };
        }

        public override async Task<GetBoolPropertyReply> CanAsymmetricBin_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = false };
        }

        public override async Task<GetBoolPropertyReply> CanGetCoolerPower_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = false };
        }

        public override async Task<GetBoolPropertyReply> CanPulseGuide_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = false };
        }

        public override async Task<GetBoolPropertyReply> CanSetCCDTemperature_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = false };
        }

        public override async Task<GetBoolPropertyReply> CanStopExposure_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = false };
        }

        public override async Task<GetDoublePropertyReply> CCDTemperature_get(
            Empty request,
            ServerCallContext context) {
            return new GetDoublePropertyReply() { Value = this.camera.Temperature };
        }

        public override async Task<GetBoolPropertyReply> CoolerOn_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = this.camera.CoolerOn };
        }

        public override async Task<Empty> CoolerOn_set(
            SetBoolPropertyRequest request,
            ServerCallContext context) {
            this.camera.CoolerOn = request.Value;
            return new Empty();
        }

        public override async Task<GetDoublePropertyReply> CoolerPower_get(
            Empty request,
            ServerCallContext context) {
            return new GetDoublePropertyReply() { Value = this.camera.CoolerPower };
        }

        public override async Task<GetDoublePropertyReply> ElectronsPerADU_get(
            Empty request,
            ServerCallContext context) {
            return new GetDoublePropertyReply() { Value = this.camera.ElectronsPerADU };
        }

        public override async Task<GetBoolPropertyReply> FastReadout_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = false };
        }

        public override async Task<Empty> FastReadout_set(
            SetBoolPropertyRequest request,
            ServerCallContext context) {
            throw new ASCOM.NotImplementedException();
        }

        public override async Task<GetDoublePropertyReply> FullWellCapacity_get(
            Empty request,
            ServerCallContext context) {
            var fullWell = this.camera.ElectronsPerADU * (1 << this.camera.BitDepth);
            return new GetDoublePropertyReply() { Value = fullWell };
        }

        public override async Task<GetShortPropertyReply> GainMax_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.GainMax };
        }

        public override async Task<GetShortPropertyReply> GainMin_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.GainMin };
        }

        public override async Task<GetStringArrayReply> Gains_get(
            Empty request,
            ServerCallContext context) {
            var reply = new GetStringArrayReply();
            reply.Value.AddRange(this.camera.Gains.Select(x => x.ToString()));
            return reply;
        }

        public override async Task<GetShortPropertyReply> Gain_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.Gain };
        }

        public override async Task<Empty> Gain_set(
            SetShortPropertyRequest request,
            ServerCallContext context) {
            throw new ASCOM.NotImplementedException();
        }

        public override async Task<GetBoolPropertyReply> HasShutter_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = false };
        }

        public override async Task<GetDoublePropertyReply> HeatSinkTemperature_get(
            Empty request,
            ServerCallContext context) {
            return new GetDoublePropertyReply() { Value = this.camera.AmbientTemperature };
        }

        public override async Task<GetImageArrayReply> ImageArray_get(
            Empty request,
            ServerCallContext context) {
            var exposeTask = this.exposeAndDownloadTask;
            var imageReady = exposeTask?.Status == TaskStatus.RanToCompletion;
            if (!imageReady) {
                // ASCOM specifies this exception when no image is available
                throw new ASCOM.InvalidOperationException("No exposure is ready yet");
            }

            var downloadedData = exposeTask.Result;
            if (downloadedData == null) {
                throw new ASCOM.DriverException("Expose and download operation failed");
            }
            var numPixels = downloadedData.Properties.Height * downloadedData.Properties.Width;
            var imageData = new byte[numPixels * 2];
            Buffer.BlockCopy(downloadedData.Data.FlatArray, 0, imageData, 0, numPixels * 2);
            return new GetImageArrayReply() {
                Data = Google.Protobuf.ByteString.CopyFrom(imageData),
                Width = downloadedData.Properties.Width,
                Height = downloadedData.Properties.Height
            };
        }

        public override async Task<GetBoolPropertyReply> ImageReady_get(
            Empty request,
            ServerCallContext context) {
            var imageReady = this.exposeAndDownloadTask?.Status == TaskStatus.RanToCompletion;
            return new GetBoolPropertyReply() { Value = imageReady };
        }

        public override async Task<GetBoolPropertyReply> IsPulseGuiding_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = false };
        }

        public override async Task<GetDoublePropertyReply> LastExposureDuration_get(
            Empty request,
            ServerCallContext context) {
            if (!latestExposureDuration.HasValue) {
                throw new ASCOM.InvalidOperationException("No exposure yet");
            }
            return new GetDoublePropertyReply() { Value = latestExposureDuration.Value };
        }

        // Reports the actual exposure start in the FITS-standard CCYY-MM-DDThh:mm:ss[.sss...] format. The time must be UTC.
        public override async Task<GetStringPropertyReply> LastExposureStartTime_get(
            Empty request,
            ServerCallContext context) {
            if (!latestExposureStart.HasValue) {
                throw new ASCOM.InvalidOperationException("No exposure yet");
            }

            // TODO: Should this return an actual datetime instead, and leave the string conversion to the client?
            var formattedLatestExposureStart = latestExposureStart.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
            return new GetStringPropertyReply() { Value = formattedLatestExposureStart };
        }

        public override async Task<GetIntPropertyReply> MaxADU_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = (1 << this.camera.BitDepth - 1) };
        }

        public override async Task<GetShortPropertyReply> MaxBinX_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.MaxBinX };
        }

        public override async Task<GetShortPropertyReply> MaxBinY_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.MaxBinY };
        }

        public override async Task<GetIntPropertyReply> NumX_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = this.camera.SubSampleWidth };
        }

        public override async Task<Empty> NumX_set(
            SetIntPropertyRequest request,
            ServerCallContext context) {
            this.camera.SubSampleWidth = request.Value;
            return new Empty();
        }

        public override async Task<GetIntPropertyReply> NumY_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = this.camera.SubSampleHeight };
        }

        public override async Task<Empty> NumY_set(
            SetIntPropertyRequest request,
            ServerCallContext context) {
            this.camera.SubSampleHeight = request.Value;
            return new Empty();
        }

        public override async Task<GetIntPropertyReply> OffsetMax_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = this.camera.OffsetMax };
        }

        public override async Task<GetIntPropertyReply> OffsetMin_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = this.camera.OffsetMin };
        }

        public override async Task<GetStringArrayReply> Offsets_get(
            Empty request,
            ServerCallContext context) {
            throw new ASCOM.NotImplementedException("No offsets");
        }

        public override async Task<GetIntPropertyReply> Offset_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = this.camera.Offset };
        }

        public override async Task<GetShortPropertyReply> PercentCompleted_get(
            Empty request,
            ServerCallContext context) {
            throw new ASCOM.InvalidOperationException("PercentCompleted not supported");
        }

        public override async Task<GetDoublePropertyReply> PixelSizeX_get(
            Empty request,
            ServerCallContext context) {
            return new GetDoublePropertyReply() { Value = this.camera.PixelSizeX };
        }

        public override async Task<GetDoublePropertyReply> PixelSizeY_get(
            Empty request,
            ServerCallContext context) {
            return new GetDoublePropertyReply() { Value = this.camera.PixelSizeY };
        }

        public override async Task<GetStringArrayReply> ReadoutModes_get(
            Empty request,
            ServerCallContext context) {
            var reply = new GetStringArrayReply();
            reply.Value.AddRange(this.camera.ReadoutModes);
            return reply;
        }

        public override async Task<GetShortPropertyReply> ReadoutMode_get(
            Empty request,
            ServerCallContext context) {
            return new GetShortPropertyReply() { Value = this.camera.ReadoutMode };
        }

        public override async Task<Empty> ReadoutMode_set(
            SetShortPropertyRequest request,
            ServerCallContext context) {
            throw new ASCOM.NotImplementedException("ReadoutMode cannot be directly set. Use BinX instead to set the readout mode for binning purposes");
        }

        public override async Task<GetStringPropertyReply> SensorName_get(
            Empty request,
            ServerCallContext context) {
            return new GetStringPropertyReply() { Value = this.camera.SensorName };
        }

        public override async Task<GetSensorTypeReply> SensorType_get(
            Empty request,
            ServerCallContext context) {
            var sensorType = this.camera.SensorType;
            SensorType protoSensorType = SensorType.Monochrome;
            if ((int)sensorType <= (int)SensorType.Lrgb) {
                protoSensorType = (SensorType)(int)sensorType;
            }
            return new GetSensorTypeReply() { Value = protoSensorType };
        }

        public override async Task<GetDoublePropertyReply> SetCCDTemperature_get(
            Empty request,
            ServerCallContext context) {
            return new GetDoublePropertyReply() { Value = this.camera.TemperatureSetPoint };
        }

        public override async Task<Empty> SetCCDTemperature_set(
            SetDoublePropertyRequest request,
            ServerCallContext context) {
            if (!this.camera.CanSetTemperature) {
                throw new ASCOM.NotImplementedException("Camera does not support setting a CCD temperature");
            }
            this.camera.TemperatureSetPoint = request.Value;
            return new Empty();
        }

        public override async Task<GetIntPropertyReply> StartX_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = this.camera.SubSampleWidth };
        }

        public override async Task<Empty> StartX_set(
            SetIntPropertyRequest request,
            ServerCallContext context) {
            if (!this.camera.CanSubSample) {
                throw new ASCOM.NotImplementedException("Camera does not support sub-sampling");
            }
            this.camera.SubSampleX = request.Value;
            return new Empty();
        }

        public override async Task<GetIntPropertyReply> StartY_get(
            Empty request,
            ServerCallContext context) {
            return new GetIntPropertyReply() { Value = this.camera.SubSampleHeight };
        }

        public override async Task<Empty> StartY_set(
            SetIntPropertyRequest request,
            ServerCallContext context) {
            if (!this.camera.CanSubSample) {
                throw new ASCOM.NotImplementedException("Camera does not support sub-sampling");
            }
            this.camera.SubSampleY = request.Value;
            return new Empty();
        }

        public override Task<GetDoublePropertyReply> SubExposureDuration_get(
            Empty request,
            ServerCallContext context) {
            throw new ASCOM.NotImplementedException("SubExposureDuration not supported");
        }

        public override async Task<Empty> SubExposureDuration_set(
            SetDoublePropertyRequest request,
            ServerCallContext context) {
            throw new ASCOM.NotImplementedException("SubExposureDuration not supported");
        }

        public override async Task<GetBoolPropertyReply> CanFastReadout_get(
            Empty request,
            ServerCallContext context) {
            return new GetBoolPropertyReply() { Value = false };
        }

        public override async Task<GetDoublePropertyReply> ExposureMin_get(
            Empty request,
            ServerCallContext context) {
            return new GetDoublePropertyReply() { Value = this.camera.ExposureMin };
        }

        public override async Task<GetDoublePropertyReply> ExposureMax_get(
            Empty request,
            ServerCallContext context) {
            return new GetDoublePropertyReply() { Value = this.camera.ExposureMax };
        }

        public override async Task<GetDoublePropertyReply> ExposureResolution_get(
            Empty request,
            ServerCallContext context) {
            // TODO: Perhaps this should be on the ICamera?
            return new GetDoublePropertyReply() { Value = 0.01d };
        }

        public override async Task<Empty> Offset_set(
            SetIntPropertyRequest request,
            ServerCallContext context) {
            this.camera.Offset = request.Value;
            return new Empty();
        }
    }
}