#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace NINA.Equipment.SDK.CameraSDKs.AtikSDK {

    public class AtikCameraDll {
        private const string DLLNAME = "AtikCameras.dll";

        static AtikCameraDll() {
            DllLoader.LoadDll(Path.Combine("Atik", "Atik.Core.dll"));
            DllLoader.LoadDll(Path.Combine("Atik", DLLNAME));
        }

        public static int GetDevicesCount() {
            _ = ArtemisRefreshDevicesCount();
            int x = ArtemisDeviceCount();
            Logger.Trace($"Number of Atik Cameras: {x}");
            return x;
        }

        public static IntPtr Connect(int id) {
            Logger.Trace($"Trying to connect to Atik camera {id}");
            ArtemisRefreshDevicesCount();
            if (ArtemisDeviceIsPresent(id) && ArtemisDeviceIsCamera(id)) {
                IntPtr cameraP = ArtemisConnect(id);
                if (cameraP == IntPtr.Zero) {
                    Logger.Trace("Connection failed, retrying as wildcard");
                    cameraP = ArtemisConnect(-1);
                }
                if (cameraP == IntPtr.Zero) {
                    throw new AtikCameraException("Unable to connect to camera", MethodBase.GetCurrentMethod(), new object[] { id });
                }
                Logger.Trace($"Connected as {cameraP}");
                return cameraP;
            }
            return IntPtr.Zero;
        }

        public static bool Disconnect(IntPtr camera) {
            return ArtemisDisconnect(camera);
        }

        public static bool IsConnected(IntPtr camera) {
            if (camera != IntPtr.Zero) {
                return ArtemisIsConnected(camera);
            } else {
                return false;
            }
        }

        public static void StartExposure(IntPtr camera, double exposuretime) {
            if (camera != IntPtr.Zero) {
                CheckError(ArtemisStartExposure(camera, (float)exposuretime), MethodBase.GetCurrentMethod(), camera);
            }
        }

        public static void StartFastExposure(IntPtr camera, int milliseconds) {
            if (camera != IntPtr.Zero) {
                ArtemisStartFastExposure(camera, milliseconds);
            }
        }

        public static bool SetFastCallbackEx(IntPtr camera, ArtemisSetFastCallback func) {
            return ArtemisSetFastCallbackEx(camera, func);
        }

        public static bool HasFastMode(IntPtr camera) {
            return ArtemisHasFastMode(camera);
        }

        public static void SetSubFrame(IntPtr camera, int x, int y, int width, int height) {
            CheckError(ArtemisSubframe(camera, x, y, width, height), MethodBase.GetCurrentMethod(), camera);
        }

        public static void StartExposureMs(IntPtr camera, int ms) {
            if (camera != IntPtr.Zero) {
                CheckError(ArtemisStartExposureMS(camera, ms), MethodBase.GetCurrentMethod(), camera);
            }
        }

        public static ColorInformation GetColorInformation(IntPtr camera) {
            CheckError(ArtemisColourProperties(camera, out ArtemisColourType colorType, out var normalOffsetX, out var normalOffsetY, out var _, out var _), MethodBase.GetCurrentMethod(), camera);

            return new ColorInformation() {
                SensorType = colorType == ArtemisColourType.ARTEMIS_COLOUR_RGGB ? SensorType.RGGB : SensorType.Monochrome,
                BayerOffsetX = (short)normalOffsetX,
                BayerOffsetY = (short)normalOffsetY,
            };
        }

        public static bool ImageReady(IntPtr camera) {
            if (camera != IntPtr.Zero) {
                return ArtemisImageReady(camera);
            } else {
                throw new Exception("Atik Camera not connected");
            }
        }

        public static double CoolerPower(IntPtr camera) {
            CheckError(ArtemisCoolingInfo(camera, out var _, out var level, out var _, out var _, out var _), MethodBase.GetCurrentMethod(), camera);
            return level / 2.55d;
        }

        public static ArtemisCameraStateEnum CameraState(IntPtr camera) {
            return ArtemisCameraState(camera);
        }

        public static bool Shutdown() {
            return ArtemisShutdown();
        }

        public static IExposureData DownloadExposure(IntPtr camera, int bitDepth, bool isBayered, IExposureDataFactory exposureDataFactory) {
            CheckError(ArtemisGetImageData(camera, out var _, out var _, out var w, out var h, out var _, out var _), MethodBase.GetCurrentMethod(), camera);

            var ptr = ArtemisImageBuffer(camera);

            var cameraDataToManaged = new CameraDataToManaged(ptr, w, h, bitDepth, bitScaling: false);
            var arr = cameraDataToManaged.GetData();

            return exposureDataFactory.CreateImageArrayExposureData(
                    input: arr,
                    width: w,
                    height: h,
                    bitDepth: bitDepth,
                    isBayered: isBayered,
                    metaData: new ImageMetaData());
        }

        private static void CopyToUShort(IntPtr source, ushort[] destination, int startIndex, int length) {
            unsafe {
                var sourcePtr = (ushort*)source;
                for (int i = startIndex; i < startIndex + length; ++i) {
                    destination[i] = *sourcePtr++;
                }
            }
        }

        public static void StopExposure(IntPtr camera) {
            if (camera != IntPtr.Zero) {
                CheckError(ArtemisStopExposure(camera), MethodBase.GetCurrentMethod(), camera);
            }
        }

        public static void AbortExposure(IntPtr camera) {
            if (camera != IntPtr.Zero) {
                CheckError(ArtemisAbortExposure(camera), MethodBase.GetCurrentMethod(), camera);
            }
        }

        public static void SetDarkMode(IntPtr camera, bool enabled) {
            if (camera != IntPtr.Zero) {
                CheckError(ArtemisSetDarkMode(camera, enabled), MethodBase.GetCurrentMethod(), camera);
            }
        }

        public static void SetBinning(IntPtr camera, int x, int y) {
            CheckError(ArtemisBin(camera, x, y), MethodBase.GetCurrentMethod(), camera);
        }

        public static void GetBinning(IntPtr camera, out int x, out int y) {
            CheckError(ArtemisGetBin(camera, out x, out y), MethodBase.GetCurrentMethod(), camera);
        }

        public static void GetMaxBinning(IntPtr camera, out int x, out int y) {
            CheckError(ArtemisGetMaxBin(camera, out x, out y), MethodBase.GetCurrentMethod(), camera);
        }

        public static void SetArtemisPreview(IntPtr camera, bool enabled) {
            ArtemisSetPreview(camera, enabled);
        }

        public static double GetSetpoint(IntPtr camera) {
            CheckError(ArtemisCoolingInfo(camera, out var _, out var _, out var _, out var _, out var setPoint), MethodBase.GetCurrentMethod(), camera);
            return setPoint / 100.0d;
        }

        public static int GetCoolingFlags(IntPtr camera) {
            CheckError(ArtemisCoolingInfo(camera, out var flags, out var _, out var _, out var _, out var _), MethodBase.GetCurrentMethod(), camera);
            return flags;
        }

        public static void SetWindowHeaterPower(IntPtr camera, int windowHeaterPower) {
            CheckError(ArtemisSetWindowHeaterPower(camera, windowHeaterPower), MethodBase.GetCurrentMethod(), camera);
        }

        public static int GetSerialNumber(IntPtr camera) {
            CheckError(ArtemisCameraSerial(camera, out var flags, out int serial), MethodBase.GetCurrentMethod(), camera);
            return serial;
        }

        public static double GetTemperature(IntPtr camera) {
            CheckError(ArtemisTemperatureSensorInfo(camera, 0, out var sensors), MethodBase.GetCurrentMethod(), camera);
            if (sensors > 0) {
                CheckError(ArtemisTemperatureSensorInfo(camera, 1, out var temperature), MethodBase.GetCurrentMethod(), camera);
                return temperature / 100.0d;
            } else {
                return double.NaN;
            }
        }

        public static void SetCooling(IntPtr camera, double setPoint) {
            CheckError(ArtemisSetCooling(camera, (int)(setPoint * 100)), MethodBase.GetCurrentMethod(), camera);
        }

        public static void SetWarmup(IntPtr camera) {
            CheckError(ArtemisCoolerWarmUp(camera), MethodBase.GetCurrentMethod(), camera);
        }

        public static ArtemisPropertiesStruct GetCameraProperties(int cameraId) {
            var handle = Connect(cameraId);
            ArtemisPropertiesStruct outstruct = GetCameraProperties(handle);
            Disconnect(handle);
            return outstruct;
        }

        public static bool HasCameraSpecificOption(IntPtr camera, AtikCameraSpecificOptions id) {
            return ArtemisHasCameraSpecificOption(camera, id);
        }

        public static void SetAmplifierSwitched(IntPtr camera, bool isOn) {
            CheckError(ArtemisSetAmplifierSwitched(camera, isOn), MethodBase.GetCurrentMethod(), camera);
        }

        public static void CameraSpecificOptionGetData(IntPtr camera, AtikCameraSpecificOptions id, ref byte[] data) {
            int length = 0;
            CheckError(ArtemisCameraSpecificOptionGetData(camera, id, data, data.Length, ref length), MethodBase.GetCurrentMethod(), camera);
        }

        public static void CameraSpecificOptionSetData(IntPtr camera, AtikCameraSpecificOptions id, byte[] data) {
            CheckError(ArtemisCameraSpecificOptionSetData(camera, id, data, data.Length), MethodBase.GetCurrentMethod(), camera);
        }

        public static IntPtr ConnectEfw(int id) {
            Logger.Trace($"Trying to connect to Atik FW {id}");
            if (ArtemisEfwIsPresent(id)) {
                IntPtr fwP = ArtemisEfwConnect(id);
                if (fwP == IntPtr.Zero) {
                    Logger.Trace("Connection failed, retrying as wildcard");
                    fwP = ArtemisEfwConnect(-1);
                }
                if (fwP == IntPtr.Zero) {
                    throw new AtikCameraException("Unable to connect to FW", MethodBase.GetCurrentMethod(), new object[] { id });
                }
                Logger.Trace($"Connected as {fwP}");
                return fwP;
            }
            return IntPtr.Zero;
        }

        public static void DisconnectEfw(IntPtr fw) {
            CheckError(ArtemisEfwDisconnect(fw), MethodBase.GetCurrentMethod(), fw);
        }

        public static bool IsConnectedEfw(IntPtr fw) {
            if (fw != IntPtr.Zero) {
                return ArtemisEfwIsConnected(fw);
            } else {
                return false;
            }
        }

        public static int GetEfwPositions(IntPtr fw) {
            CheckError(ArtemisEfwNmrPositions(fw, out int positions), MethodBase.GetCurrentMethod(), fw);
            return positions;
        }

        public static short GetCurrentEfwPosition(IntPtr fw) {
            CheckError(ArtemisEfwGetPosition(fw, out int position, out bool _), MethodBase.GetCurrentMethod(), fw);
            return (short)position;
        }

        public static bool GetCurrentEfwMoving(IntPtr fw) {
            CheckError(ArtemisEfwGetPosition(fw, out int _, out bool moving), MethodBase.GetCurrentMethod(), fw);
            return moving;
        }

        public static void SetCurrentEfwPosition(IntPtr fw, int position) {
            CheckError(ArtemisEfwSetPosition(fw, position), MethodBase.GetCurrentMethod(), fw);
        }

        public static ArtemisEfwType GetArtemisEfwType(int deviceId) {
            CheckError(ArtemisEfwGetDeviceDetails(deviceId, out var type, out char _), MethodBase.GetCurrentMethod(), deviceId);
            return type;
        }

        public static char GetArtemisEfwSerial(int deviceid) {
            CheckError(ArtemisEfwGetDeviceDetails(deviceid, out var _, out char serial), MethodBase.GetCurrentMethod(), deviceid);
            return serial;
        }

        public static ArtemisEfwType GetConnectedArtemisEfwType(IntPtr fw) {
            CheckError(ArtemisEfwGetDetails(fw, out var type, out char _), MethodBase.GetCurrentMethod(), fw);
            return type;
        }

        public static char GetConnectedArtemisEfwSerial(IntPtr fw) {
            CheckError(ArtemisEfwGetDetails(fw, out var _, out char serial), MethodBase.GetCurrentMethod(), fw);
            return serial;
        }

        public static ArtemisPropertiesStruct GetCameraProperties(IntPtr camera) {
            SensorType type = GetColorInformation(camera).SensorType;
            ArtemisPropertiesStruct outstruct = new ArtemisPropertiesStruct();
            CheckError(ArtemisProperties(camera, ref outstruct), MethodBase.GetCurrentMethod(), camera);

            if (type == SensorType.RGGB) {
                // Debayering doesn't seem to handle uneven rows or columns
                if ((outstruct.nPixelsX % 1) == 0) {
                    outstruct.nPixelsX--;
                }
                if ((outstruct.nPixelsY % 1) == 0) {
                    outstruct.nPixelsY--;
                }
            }
            return outstruct;
        }

        public static int GetInternalFilterWheelPositions(IntPtr camera) {
            if (camera != IntPtr.Zero) {
                CheckError(ArtemisFilterWheelInfo(camera, out int wheelCount, out int _, out int _, out int _), MethodBase.GetCurrentMethod(), camera);
                return wheelCount;
            } else {
                return -1;
            }
        }

        public static bool GetInternalFilterWheelIsMoving(IntPtr camera) {
            if (camera != IntPtr.Zero) {
                CheckError(ArtemisFilterWheelInfo(camera, out int _, out int moving, out int _, out int _), MethodBase.GetCurrentMethod(), camera);
                return moving != 0;
            } else {
                return false;
            }
        }

        public static short GetInternalFilterWheelCurrentPosition(IntPtr camera) {
            if (camera != IntPtr.Zero) {
                CheckError(ArtemisFilterWheelInfo(camera, out int _, out int _, out int curentPos, out int _), MethodBase.GetCurrentMethod(), camera);
                return (short)curentPos;
            } else {
                return -1;
            }
        }

        public static void SetInternalFilterWheelTargetPosition(IntPtr camera, int position) {
            if (camera != IntPtr.Zero) {
                CheckError(ArtemisFilterWheelMove(camera, position), MethodBase.GetCurrentMethod(), camera);
            }
        }

        public static string DriverVersion {
            get {
                return DllLoader.DllVersion("Atik/" + DLLNAME).ProductVersion;
            }
        }

        public static string DriverName {
            get {
                return DllLoader.DllVersion("Atik/" + DLLNAME).ProductName;
            }
        }

        public static string GetDeviceName(int cameraId) {
            StringBuilder cameraName = new StringBuilder();
            ArtemisDeviceName(cameraId, cameraName);
            return cameraName.ToString();
        }

        public static string GetDeviceSerialNumber(int cameraId) {
            StringBuilder cameraSerial = new StringBuilder();
            ArtemisDeviceSerial(cameraId, cameraSerial);
            return cameraSerial.ToString();
        }

        /// <summary>
        /// Returns the version of the API. This number comes from the services itself.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisAPIVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ArtemisAPIVersion();

        /// <summary>
        /// Returns the version of the DLL. This number is set in the DLL. It is important to check
        /// that the DLL and API version match. If not, there is a strong possibility that things
        /// won't work properly.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDLLVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ArtemisDLLVersion();

        /// <summary>
        /// Returns a bool to indicate whether the given device is present.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceIsPresent", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ArtemisDeviceIsPresent(int iDevice);

        /// <summary>
        /// Returns a bool to indicate whether the given device is present.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDevicePresent", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ArtemisDevicePresent(int iDevice);

        /// <summary>
        /// Returns a bool to indicate whether the given device is present.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceInUse", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ArtemisDeviceInUse(int iDevice);

        /// <summary>
        /// Sets the supplied 'pName' variable to the name of the given device. Return true if
        /// iDevice is found, false otherwise.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceName", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDeviceName(int iDevice, [MarshalAs(UnmanagedType.LPStr)] StringBuilder pName);

        /// <summary>
        /// Sets the supplied 'pSerial' variable to the serial number of the given device. Returns
        /// true if iDevice is found, false otherwise.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceSerial", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDeviceSerial(int iDevice, [MarshalAs(UnmanagedType.LPStr)] StringBuilder pName);

        /// <summary>
        /// Connects to the given camera. The ArtemisHandle is actually a 'void *' and will be needed
        /// for all camera specific methods. It will return 0 if this method fails. You can call this
        /// method with '-1', in which case, you will received the first camera that is not currently
        /// in use (by any application).
        /// Note: It is possible for different applications to connect to the same camera.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisConnect", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr ArtemisConnect(int iDevice);

        /// <summary>
        /// This method is used to release the camera when done. It will allow other applications to
        /// connect to the camera as listed above.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDisconnect", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDisconnect(IntPtr camera);

        /// <summary>
        /// This method is used to release all the cameras when done. It will allow other
        /// applications to connect to the camera as listed above. Although it used to be best to
        /// call this function when the application is closing, it is no longer necessary as the DLL
        /// itself will remove all connections when shutting down.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDisconnectAll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDisconnectAll();

        /// <summary>
        /// This method is used to release all the cameras when done. It will allow other
        /// applications to connect to the camera as listed above. Although it used to be best to
        /// call this function when the application is closing, it is no longer necessary as the DLL
        /// itself will remove all connections when shutting down.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisShutdown", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisShutdown();

        /// <summary>
        /// This method is used to release all the cameras when done. It will allow other
        /// applications to connect to the camera as listed above. Although it used to be best to
        /// call this function when the application is closing, it is no longer necessary as the DLL
        /// itself will remove all connections when shutting down.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDisconnectAll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDisconnectAll(int iDevice);

        /// <summary>
        /// Used to check that the given camera is still connected. The most likely reason for this
        /// happening is that the camera has been unplugged.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisIsConnected", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisIsConnected(IntPtr camera);

        /// <summary>
        /// The refresh devices count tells you how many times the camera list has changed on the
        /// service. The camera list changes every time a USB device is connected or removed.
        /// Therefore, the purpose of this method is to tell the user that the cameras have changed
        /// and that it's worth checking to make sure their camera(s) are still connected.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisRefreshDevicesCount", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int ArtemisRefreshDevicesCount();

        /// <summary>
        /// Returns the number of connected and recognised devices. The count does not include misconfigured devices (E.G. if drivers are missing). 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceCount", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int ArtemisDeviceCount();

        /// <summary>
        /// Used to check that a device is a camera before connecting to it. The only alternative is
        /// a test bench, so this function is usually of no use.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceIsCamera", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool ArtemisDeviceIsCamera(int iDevice);

        /// <summary>
        /// This method is used to find out the serial number of the camera (as written in the
        /// EEPROM). Note that this number is likely to be different to the Device Serial which is
        /// supplied by the USB device. This method returns an ArtemisErrorCode as listed below.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCameraSerial", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisCameraSerial(IntPtr camera, out int flags, out int serial);

        /// <summary>
        /// Sets the supplied ARTEMISPROPERTIES to the value for the given camera.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisProperties", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisProperties(IntPtr camera, ref ArtemisPropertiesStruct prop);

        /// <summary>
        /// Gives the colour properties of the given camera. The offsets(Normal / Preview - X / Y)
        /// give you information about the Bayer matrix used.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisColourProperties", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisColourProperties(IntPtr camera, out ArtemisColourType colorType, out int normalOffsetX, out int normalOffsetY, out int previewOffsetX, out int previewOffsetY);

        /// <summary>
        /// As you might expect, this method is used to start an exposure on the camera. The function
        /// will return an error code as listed below.
        /// Note: If you have previously stopped the exposure, then you have to wait for the camera
        ///       state (listed below) to return to Idle before another exposure can be made.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisStartExposure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisStartExposure(IntPtr camera, float seconds);

        /// <summary>
        /// Same as Start Exposure, except the supplied time is in milliseconds instead of seconds.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisStartExposureMS", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisStartExposureMS(IntPtr camera, int milliseconds);

        /// <summary>
        /// Start continous expsures using the current camera settings. Image data is retrieved via callback.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisStartFastExposure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisStartFastExposure(IntPtr camera, int milliseconds);

        [DllImport(DLLNAME, EntryPoint = "ArtemisSetFastCallbackEx", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisSetFastCallbackEx(IntPtr camera, ArtemisSetFastCallback func);

        [DllImport(DLLNAME, EntryPoint = "ArtemisHasFastMode", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisHasFastMode(IntPtr camera);

        /// <summary>
        /// Set camera dark mode to enabled (will keep shutter closed)
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSetDarkMode", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSetDarkMode(IntPtr camera, bool enable);

        /// <summary>
        /// Used to cancel the current exposure.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisStopExposure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisStopExposure(IntPtr camera);

        /// <summary>
        /// Used to cancel the current exposure.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisAbortExposure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisAbortExposure(IntPtr camera);

        /// <summary>
        /// Let's you know when the image is ready. The value is set to 'false' when 'Start Exposure'
        /// is called and only returns true once the exposure has finished.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisImageReady", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisImageReady(IntPtr camera);

        /// <summary>
        /// Used to cancel the current exposure.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCameraState", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisCameraStateEnum ArtemisCameraState(IntPtr camera);

        /// <summary>
        /// How much of the image has been download from the sensor. Note: Some cameras will do the
        /// whole thing in one go, so the value will jump from 0% to 100%. Other take longer and will
        /// reflect how long is left
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDownloadPercent", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int ArtemisDownloadPercent(IntPtr camera);

        /// <summary>
        /// Will populate the given values with the details of the image
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisGetImageData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisGetImageData(IntPtr camera, out int x, out int y, out int w, out int h, out int binX, out int binY);

        /// <summary>
        /// Returns a pointer to the image buffer. You should call ArtemisGetImageData first to find
        /// out the dimensions of the image. This will return 0 if there is no image.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisImageBuffer", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr ArtemisImageBuffer(IntPtr camera);

        /// <summary>
        /// Used to set the binning values of the exposure. The function will return an error code as
        /// listed below.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisBin", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisBin(IntPtr camera, int x, int y);

        /// <summary>
        /// This function will populate the given parameters with the current binning values.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisGetBin", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisGetBin(IntPtr camera, out int x, out int y);

        /// <summary>
        /// This function will populate the given parameters with the maximum binning values for this camera.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisGetMaxBin", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisGetMaxBin(IntPtr camera, out int x, out int y);

        /// <summary>
        /// This function will set the subframe values
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSubframe", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSubframe(IntPtr camera, int x, int y, int w, int h);

        /// <summary>
        /// This function will set the x, y position of the subframe.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSubframePos", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSubframePos(IntPtr camera, int x, int y);

        /// <summary>
        /// This function will set the size of subframe.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSubframeSize", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSubframeSize(IntPtr camera, int x, int y);

        /// <summary>
        /// This function will populate the given values with the current subframe values.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisGetSubframe", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisGetSubframe(IntPtr camera, out int x, out int y, out int w, out int h);

        /// <summary>
        /// Preview mode will produce images at a faster rate, but at a cost of quality. This method
        /// is used to set the camera into normal / preview mode. Passing bPrev = true will set the
        /// camera into preview mode, 'bPrev = false' sets the camera into normal mode.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSetPreview", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSetPreview(IntPtr camera, bool prev);

        /// <summary>
        /// This function has two purposes. Firstly, if you call this function with 'sensor = 0',
        /// then temperature will actually be set to the number of sensors. Once you know how many
        /// sensors there are, you can call this method with a '1-based' sensor index, and
        /// temperature will be set to the temperature reading of that sensor. The temperature is in
        /// 1/100 of a degree (Celcius), so a value of -1000 is actually -10C
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisTemperatureSensorInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisTemperatureSensorInfo(IntPtr camera, int sensor, out int temperature);

        /// <summary>
        /// This function is used to set the temperature of the camera. The setpoint is in 1/100 of a
        /// degree (Celcius). So, to set the cooling to -10C, you need to call the function with
        /// setpoint = -1000.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSetCooling", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSetCooling(IntPtr camera, int setPoint);

        /// <summary>
        /// Gives the current state of the cooling.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCoolingInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisCoolingInfo(IntPtr camera, out int flags, out int level, out int minlvl, out int maxlvl, out int setPoint);

        /// <summary>
        /// Tells the camera to start warming up.
        /// Note: It is very important that this function is called at the end of operation. Letting
        /// the sensor warm up naturally can cause damage to the sensor. It's not unusual for the
        /// temperature to go further down before going up.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCoolerWarmUp", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisCoolerWarmUp(IntPtr camera);

        /// <summary>
        /// Returns whether the specified option is available.
        /// </summary>
        /// <param name="camera">the connected Atik device handle.</param>
        /// <param name="id">the camera specific option</param>
        /// <returns>true if supported, false if not.</returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisHasCameraSpecificOption", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisHasCameraSpecificOption(IntPtr camera, AtikCameraSpecificOptions id);

        /// <summary>
        /// Used to get the specified option's current value. Please check that the current camera has this option using ArtemisHasCameraSpecificOption()
        /// </summary>
        /// <param name="camera">the connected Atik device handle.</param>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <param name="dataLength"></param>
        /// <param name="actualLength"></param>
        /// <returns>ARTEMIS_OK on success, ARTEMIS_INVALID_PARAM if the opton is not available or ARTEMISERROR on failure</returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCameraSpecificOptionGetData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisCameraSpecificOptionGetData(IntPtr camera, AtikCameraSpecificOptions id, [In, Out] byte[] data, int dataLength, ref int actualLength);

        /// <summary>
        /// Used to set the specified option's value. Please check that the current camera has this option using ArtemisHasCameraSpecificOption()
        /// </summary>
        /// <param name="camera">the connected Atik device handle.</param>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <param name="dataLength"></param>
        /// <returns>ARTEMIS_OK on success, ARTEMIS_INVALID_PARAM if the opton is not available or ARTEMISERROR on failure</returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCameraSpecificOptionSetData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisCameraSpecificOptionSetData(IntPtr camera, AtikCameraSpecificOptions id, [In, Out] byte[] data, int dataLength);

        /// <summary>
        /// Gets the window heater power.
        /// </summary>
        /// <param name="camera">the connected Atik device handle</param>
        /// <param name="windowHeaterPower">a pointer to an integer, which will be set to the current window heater power, between 0 and 255.</param>
        /// <returns>ARTEMIS_OK on success, ARTEMARTEMIS_INVALID_PARAMETER if the device does not have a window heater, or ARTEMISERROR enumeration on failure</returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisGetWindowHeaterPower", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisGetWindowHeaterPower(IntPtr camera, [Out] int windowHeaterPower);

        /// <summary>
        /// Sets the window heater power.
        /// </summary>
        /// <param name="camera">the connected Atik device handle</param>
        /// <param name="windowHeaterPower">A value between 0 and 255 specifying the power to the window heater</param>
        /// <returns>ARTEMIS_OK on success, ARTEMARTEMIS_INVALID_PARAMETER if the device does not have a window heater, or ARTEMISERROR enumeration on failure</returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSetWindowHeaterPower", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSetWindowHeaterPower(IntPtr camera, int windowHeaterPower);

        /// <summary>
        /// Sets amplifier state
        /// </summary>
        /// <param name="camera">the connected Atik device handle</param>
        /// <param name="isOn"><see langword="true"/> for on, <see langword="false"/> for off</param>
        /// <returns>ARTEMIS_OK on success, ARTEMARTEMIS_INVALID_PARAMETER if the device does not have a window heater, or ARTEMISERROR enumeration on failure</returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSetAmplifierSwitched", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSetAmplifierSwitched(IntPtr camera, bool isOn);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ArtemisSetFastCallback(IntPtr camera, int x, int y, int w, int h, int binx, int biny, IntPtr imageBuffer, IntPtr info);

        /*
         * Not yet added: Internal and External Filter Wheel and Guiding Methods.
         */

        /// <summary>
        /// Check if an EFW is present on this device id
        /// </summary>
        /// <param name="deviceNr"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisEFWIsPresent", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern bool ArtemisEfwIsPresent(int deviceId);

        /// <summary>
        /// Get device details of an artemis EFW
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="type"></param>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisEFWGetDeviceDetails", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisEfwGetDeviceDetails(int deviceId, out ArtemisEfwType type, out char serialNumber);

        /// <summary>
        /// Get device details of an connected artemis EFW
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="type"></param>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisEFWGetDetails", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisEfwGetDetails(IntPtr device, out ArtemisEfwType type, out char serialNumber);

        /// <summary>
        /// Connect to device with specified device id
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisEFWConnect", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr ArtemisEfwConnect(int deviceId);

        /// <summary>
        /// Disconnects connected device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisEFWDisconnect", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisEfwDisconnect(IntPtr device);

        /// <summary>
        /// Gets the number of filterwheel positions
        /// </summary>
        /// <param name="device"></param>
        /// <param name="positions"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisEFWNmrPosition", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisEfwNmrPositions(IntPtr device, out int positions);

        /// <summary>
        /// Gets the current position of the filterwheel + moving state
        /// </summary>
        /// <param name="device"></param>
        /// <param name="position"></param>
        /// <param name="moving"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisEFWGetPosition", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisEfwGetPosition(IntPtr device, out int position, out bool moving);

        /// <summary>
        /// Gets the current position of the filterwheel + moving state
        /// </summary>
        /// <param name="device"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisEFWSetPosition", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisEfwSetPosition(IntPtr device, int position);

        /// <summary>
        /// Checks if the device is connected
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisEFWIsConnected", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool ArtemisEfwIsConnected(IntPtr device);

        /// <summary>
        /// Information about internal filter wheel
        /// </summary>
        /// <param name="device"></param>
        /// <param name="filterNumbers"></param>
        /// <param name="moving"></param>
        /// <param name="currentPos"></param>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisFilterWheelInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisFilterWheelInfo(IntPtr device, out int filterNumbers, out int moving, out int currentPos, out int targetPos);

        /// <summary>
        /// Moves internal filter wheel
        /// </summary>
        /// <param name="device"></param>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "ArtemisFilterWheelMove", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ArtemisErrorCode ArtemisFilterWheelMove(IntPtr device, int targetPos);

        public enum ArtemisCameraStateEnum {

            /// <summary>
            /// The default state of the camera. Indicates that the camera is ready to start and exposure
            /// </summary>
            CAMERA_IDLE,

            /// <summary>
            /// This is a temporary state between receiving a start exposure command, and the
            /// exposure actually starting.
            /// </summary>
            CAMERA_WAITING,

            /// <summary>
            /// The camera is exposing
            /// </summary>
            CAMERA_EXPOSING,

            /// <summary>
            /// The service is reading the image off the sensor
            /// </summary>
            CAMERA_DOWNLOADING,

            /// <summary>
            /// Stop Exposure has been called, and we are waiting for everything to stop. Some
            /// cameras will still need to clear the sensor, so this can take a while.
            /// </summary>
            CAMERA_FLUSHING,

            /// <summary>
            /// Something has gone wrong with the camera
            /// </summary>
            CAMERA_ERROR = -1
        }

        private enum ArtemisColourType {

            /// <summary>
            /// Unknown colour type
            /// </summary>
            ARTEMIS_COLOUR_UNKNOWN,

            /// <summary>
            /// No colour (Mono camera)
            /// </summary>
            ARTEMIS_COLOUR_NONE,

            /// <summary>
            /// Colour Camera (Bayer Matrix)
            /// </summary>
            ARTEMIS_COLOUR_RGGB
        }

        public enum ArtemisErrorCode {

            /// <summary>
            /// The function call has been successful
            /// </summary>
            ARTEMIS_OK,

            /// <summary>
            /// One or more of the parameters supplied are inconsistent with what was expected. One
            /// example of this would be calling any camera function with an invalid ArtemisHandle.
            /// Another might be to set a subframe to an unreachable area.
            /// </summary>
            ARTEMIS_INVALID_PARAMETER,

            /// <summary>
            /// Returned when calling a camera function on a camera which is no longer connected.
            /// </summary>
            ARTEMIS_NOT_CONNECTED,

            /// <summary>
            /// Returned for functions that are no longer used.
            /// </summary>
            ARTEMIS_NOT_IMPLEMENTED,

            /// <summary>
            /// Returned if a function times out for any reason
            /// </summary>
            ARTEMIS_NO_RESPONSE,

            /// <summary>
            /// Returned when trying to call a camera specific function on a camera which doesn't
            /// have that feature. (Such as the cooling functions on cameras without cooling).
            /// </summary>
            ARTEMIS_INVALID_FUNCTION,

            /// <summary>
            /// Returned if trying to call a function on something that hasn't been initialised. The
            /// only current example is the lens control
            /// </summary>
            ARTEMIS_NOT_INITIALISED,

            /// <summary>
            /// Returned if a function couldn't complete for any other reason
            /// </summary>
            ARTEMIS_OPERATION_FAILED,

            /// <summary>
            /// Returned if a function login in
            /// </summary>
            ARTEMIS_INVALID_PASSWORD,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 188)]
        public struct ArtemisPropertiesStruct {

            /// <summary>
            /// The firmware version of the camera
            /// </summary>
            public int Protocol;

            /// <summary>
            /// The width of the sensor in pixels
            /// </summary>
            public int nPixelsX;

            /// <summary>
            /// The height of the sensor in pixels
            /// </summary>
            public int nPixelsY;

            /// <summary>
            /// The width of each pixel in microns
            /// </summary>
            public float PixelMicronsX;

            /// <summary>
            /// The height of each pixel in microns
            /// </summary>
            public float PixelMicronsY;

            /// <summary>
            /// Represents the properties of the CCD
            /// <seealso cref="ArtemisPropertiesCcdFlags"/>
            /// </summary>
            public int ccdflags;

            /// <summary>
            /// Represents the properties of the camera
            /// <seealso cref="ArtemisPropertiesCameraFlags"/>
            /// </summary>
            public int cameraflags;

            /// <summary>
            /// The name of the type of camera
            /// </summary>
            //[MarshalAs(UnmanagedType.LPStr)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public char[] Description;

            /// <summary>
            /// The manufacturer of the camera. Usually Atik Cameras.
            /// </summary>
            //[MarshalAs(UnmanagedType.LPStr)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public char[] Manufacturer;
        }

        [Flags]
        public enum ArtemisPropertiesCcdFlags : int {
            /// CCD is interlaced type
            Interlaced = 0x1,
        }

        [Flags]
        public enum ArtemisPropertiesCameraFlags : int {
            /// Camera has readout FIFO fitted
            Fifo = 0x1,

            /// Camera has external trigger capabilities
            ExtTrigger = 0x2,

            /// Camera can return preview data
            Preview = 0x4,

            /// Camera can return subsampled data
            Subsample = 0x8,

            /// Camera has a mechanical shutter
            HasShutter = 0x10,

            /// Camera has a guide port
            HasGuidePort = 0x20,

            /// Camera has GPIO capability
            HasGpio = 0x40,

            /// Camera has a window heater
            HasWindowHeater = 0x80,

            /// Camera can download 8-bit images
            HasEightBitMode = 0x100,

            /// Camera can overlap
            HasOverlapMode = 0x200,

            /// Camera has internal filterwheel
            HasFilterWheel = 0x400,
        }

        [Flags]
        public enum ArtemisCoolingInfoFlags : int {
            /// Camera can be cooled. 0= No cooling ability 1= Has cooling
            HasCooling = 0x1,

            /// Cooling is always on or can be controlled. 0= Always on 1= Controllable
            Controllable = 0x2,

            /// Cooling can be switched On/Off. 0= On/Off control not available 1= On/Off control available
            OnOffCoolingControl = 0x4,

            /// Cooling can be set via ArtemisSetCoolingPower()
            PowerLeveLControl = 0x8,

            /// Cooling can be set via ArtemisSetCooling()
            SetpointControl = 0x10,

            /// Currently warming up. 0= Normal control 1= Warming Up
            WarmingUp = 0x20,

            /// Currently cooling. 0= Cooling off 1= Cooling on
            CoolingOn = 0x40,

            /// Currently under setpoint control 0= No set point control 1= Set point control
            SetpointControlOn = 0x80,
        }

        public enum ArtemisEfwType {
            NONE = 0,
            EFW1,
            EFW2,
            IFW,
        }

        public enum AtikCameraSpecificOptions : ushort {
            ID_GOPresetMode = 1,
            ID_GOPresetLow,
            ID_GOPresetMed,
            ID_GOPresetHigh,
            ID_GOCustomGain,
            ID_GOCustomOffset,

            ID_EvenIllumination = 12,
            ID_PadData,
            ID_ExposureSpeed,
            ID_BitSendMode,

            ID_FX3Version = 200,
            ID_FPGAVersion,
        }

        public enum ArtemisCoolingStatus {
            Off = 0,
            Cooling,
            WarmingUp,
            Error,
            Unknown
        }

        public enum ArtemisCoolingType {
            None = 0,
            OnOff,
            Power,
            SetPoint,
            Unknown
        }

        public enum ArtemisPrechargeMode {
            None = 0,
            ICPS,
            Full,
        }

        public enum HotPixelSensitivity {
            HPS_HIGH = 0,
            HPS_MEDIUM,
            HPS_LOW
        }

        public class ColorInformation {
            public SensorType SensorType { get; set; } = SensorType.Monochrome;
            public short BayerOffsetX { get; set; } = 0;
            public short BayerOffsetY { get; set; } = 0;
        }

        public class PresetInformation {
            public AtikCameraSpecificOptions Id { get; set; } = 0;
            public string Name { get; set; } = string.Empty;
            public ushort Gain { get; set; } = 0;
            public ushort Offset { get; set; } = 0;
        }

        private static void CheckError(ArtemisErrorCode code, MethodBase callingMethod, params object[] parameters) {
            switch (code) {
                case ArtemisErrorCode.ARTEMIS_OK:
                    break;

                case ArtemisErrorCode.ARTEMIS_NO_RESPONSE:
                    throw new AtikCameraException("Atik Camera Operation timed out", callingMethod, parameters);
                case ArtemisErrorCode.ARTEMIS_OPERATION_FAILED:
                    throw new AtikCameraException("Atik Camera Operation failed", callingMethod, parameters);
                case ArtemisErrorCode.ARTEMIS_NOT_IMPLEMENTED:
                    throw new AtikCameraException("Atik Camera method not implemented", callingMethod, parameters);
                case ArtemisErrorCode.ARTEMIS_NOT_CONNECTED:
                    throw new AtikCameraException("Atik Camera not connected", callingMethod, parameters);
                case ArtemisErrorCode.ARTEMIS_INVALID_PARAMETER:
                    throw new AtikCameraException("Atik Camera invalid parameter for method", callingMethod, parameters);
                case ArtemisErrorCode.ARTEMIS_INVALID_FUNCTION:
                    throw new AtikCameraException("Atik Camera invalid method", callingMethod, parameters);
                case ArtemisErrorCode.ARTEMIS_NOT_INITIALISED:
                    throw new AtikCameraException("Atik Camera method not initialized", callingMethod, parameters);
                case ArtemisErrorCode.ARTEMIS_INVALID_PASSWORD:
                    throw new AtikCameraException("Atik Camera password failure", callingMethod, parameters);

                default:
                    throw new ArgumentOutOfRangeException("Atik Camera method error code");
            }
        }

        public class AtikCameraException : Exception {

            public AtikCameraException(string message, MethodBase callingMethod, object[] parameters) : base(CreateMessage(message, callingMethod, parameters)) {
            }

            private static string CreateMessage(string message, MethodBase callingMethod, object[] parameters) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error '" + message + "' from call to ");
                sb.Append("Atik" + callingMethod.Name + "(");
                var paramNames = callingMethod.GetParameters().Select(x => x.Name);
                foreach (var line in paramNames.Zip(parameters, (s, o) => string.Format("{0}={1}, ", s, o))) {
                    sb.Append(line);
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(")");
                return sb.ToString();
            }
        }
    }
}