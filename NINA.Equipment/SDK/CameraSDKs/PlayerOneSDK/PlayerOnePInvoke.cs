#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK {

    public class PlayerOnePInvokeProxy : IPlayerOnePInvokeProxy {

        [SecurityCritical]
        public int POAGetCameraCount() {
            return PlayerOnePInvoke.POAGetCameraCount();
        }

        [SecurityCritical]
        public POAErrors POAGetCameraProperties(int nIndex, out POACameraProperties pProp) {
            return PlayerOnePInvoke.POAGetCameraProperties(nIndex, out pProp);
        }

        [SecurityCritical]
        public POAErrors POAGetCameraPropertiesByID(int nCameraID, out POACameraProperties pProp) {
            return PlayerOnePInvoke.POAGetCameraPropertiesByID(nCameraID, out pProp);
        }

        [SecurityCritical]
        public POAErrors POAOpenCamera(int nCameraID) {
            return PlayerOnePInvoke.POAOpenCamera(nCameraID);
        }

        [SecurityCritical]
        public POAErrors POAInitCamera(int nCameraID) {
            return PlayerOnePInvoke.POAInitCamera(nCameraID);
        }

        [SecurityCritical]
        public POAErrors POACloseCamera(int nCameraID) {
            return PlayerOnePInvoke.POACloseCamera(nCameraID);
        }

        [SecurityCritical]
        public POAErrors POAGetConfigsCount(int nCameraID, out int pConfCount) {
            return PlayerOnePInvoke.POAGetConfigsCount(nCameraID, out pConfCount);
        }

        [SecurityCritical]
        public POAErrors POAGetConfigAttributes(int nCameraID, int nConfIndex, out POAConfigAttributes pConfAttr) {
            return PlayerOnePInvoke.POAGetConfigAttributes(nCameraID, nConfIndex, out pConfAttr);
        }

        [SecurityCritical]
        public POAErrors POAGetConfigAttributesByConfigID(int nCameraID, POAConfig confID, out POAConfigAttributes pConfAttr) {
            return PlayerOnePInvoke.POAGetConfigAttributesByConfigID(nCameraID, confID, out pConfAttr);
        }

        [SecurityCritical]
        public POAErrors POASetConfig(int nCameraID, POAConfig confID, POAConfigValue confValue, POABool isAuto) {
            return PlayerOnePInvoke.POASetConfig(nCameraID, confID, confValue, isAuto);
        }

        [SecurityCritical]
        public POAErrors POAGetConfig(int nCameraID, POAConfig confID, out POAConfigValue confValue, out POABool isAuto) {
            return PlayerOnePInvoke.POAGetConfig(nCameraID, confID, out confValue, out isAuto);
        }

        [SecurityCritical]
        public POAErrors POAGetConfigValueType(POAConfig confID, out POAValueType pConfValueType) {
            return PlayerOnePInvoke.POAGetConfigValueType(confID, out pConfValueType);
        }

        [SecurityCritical]
        public POAErrors POASetImageStartPos(int nCameraID, int startX, int startY) {
            return PlayerOnePInvoke.POASetImageStartPos(nCameraID, startX, startY);
        }
        [SecurityCritical]

        public POAErrors POAGetImageStartPos(int nCameraID, out int pStartX, out int pStartY) {
            return PlayerOnePInvoke.POAGetImageStartPos(nCameraID, out pStartX, out pStartY);
        }

        [SecurityCritical]
        public POAErrors POASetImageSize(int nCameraID, int width, int height) {
            return PlayerOnePInvoke.POASetImageSize(nCameraID, width, height);
        }

        [SecurityCritical]
        public POAErrors POAGetImageSize(int nCameraID, out int pWidth, out int pHeight) {
            return PlayerOnePInvoke.POAGetImageSize(nCameraID, out pWidth, out pHeight);
        }

        [SecurityCritical]
        public POAErrors POASetImageBin(int nCameraID, int bin) {
            return PlayerOnePInvoke.POASetImageBin(nCameraID, bin);
        }

        [SecurityCritical]
        public POAErrors POAGetImageBin(int nCameraID, out int pBin) {
            return PlayerOnePInvoke.POAGetImageBin(nCameraID, out pBin);
        }

        [SecurityCritical]
        public POAErrors POASetImageFormat(int nCameraID, POAImgFormat imgFormat) {
            return PlayerOnePInvoke.POASetImageFormat(nCameraID, imgFormat);
        }

        [SecurityCritical]
        public POAErrors POAGetImageFormat(int nCameraID, out POAImgFormat pImgFormat) {
            return PlayerOnePInvoke.POAGetImageFormat(nCameraID, out pImgFormat);
        }

        [SecurityCritical]
        public POAErrors POAStartExposure(int nCameraID, POABool bSignalFrame) {
            return PlayerOnePInvoke.POAStartExposure(nCameraID, bSignalFrame);
        }

        [SecurityCritical]
        public POAErrors POAStopExposure(int nCameraID) {
            return PlayerOnePInvoke.POAStopExposure(nCameraID);
        }

        [SecurityCritical]
        public POAErrors POAGetCameraState(int nCameraID, out POACameraState pCameraState) {
            return PlayerOnePInvoke.POAGetCameraState(nCameraID, out pCameraState);
        }

        [SecurityCritical]
        public POAErrors POAGetImageData(int nCameraID, [Out] ushort[] pBuf, int nBufSize, int nTimeoutms) {
            return PlayerOnePInvoke.POAGetImageData(nCameraID, pBuf, nBufSize, nTimeoutms);
        }

        [SecurityCritical]
        public POAErrors POAGetDroppedImagesCount(int nCameraID, out int pDroppedCount) {
            return PlayerOnePInvoke.POAGetDroppedImagesCount(nCameraID, out pDroppedCount);
        }

        [SecurityCritical]
        public POAErrors POASetUserCustomID(int nCameraID, IntPtr pCustomID, int len) {
            return PlayerOnePInvoke.POASetUserCustomID(nCameraID, pCustomID, len);
        }

        [SecurityCritical]
        public POAErrors POAGetGainOffset(int nCameraID, out int pOffsetHighestDR, out int pOffsetUnityGain, out int pGainLowestRN, out int pOffsetLowestRN, out int pHCGain) {
            return PlayerOnePInvoke.POAGetGainOffset(nCameraID, out pOffsetHighestDR, out pOffsetUnityGain, out pGainLowestRN, out pOffsetLowestRN, out pHCGain);
        }

        [SecurityCritical]
        public POAErrors POAImageReady(int nCameraID, out POABool pIsReady) {
            return PlayerOnePInvoke.POAImageReady(nCameraID, out pIsReady);
        }

        [SecurityCritical]
        public POAErrors POASetTrgModeEnable(int nCameraId, POABool enable) {
            return PlayerOnePInvoke.POASetTrgModeEnable(nCameraId, enable);
        }

        [SecurityCritical]
        public IntPtr POAGetErrorString(POAErrors err) {
            return PlayerOnePInvoke.POAGetErrorString(err);
        }

        [SecurityCritical]
        public int POAGetAPIVersion() {
            return PlayerOnePInvoke.POAGetAPIVersion();
        }

        [SecurityCritical]
        public string POAGetSDKVersion() {
            IntPtr p = PlayerOnePInvoke.POAGetSDKVersion();
            string version = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(p);

            return version;
        }

        //overload POASetConfig begin-------
        public static POAErrors POASetConfig(int nCameraID, POAConfig confID, int nValue, bool isAuto) {
            POAValueType pConfValueType; // Must all variables be placed in 64 processes???
            POAErrors error = PlayerOnePInvoke.POAGetConfigValueType(confID, out pConfValueType);
            if (error == POAErrors.POA_OK) {
                if (pConfValueType != POAValueType.VAL_INT) {
                    return POAErrors.POA_ERROR_INVALID_CONFIG;
                }
            } else {
                return error;
            }
            POAConfigValue confValue = new POAConfigValue();
            confValue.intValue = nValue;

            return PlayerOnePInvoke.POASetConfig(nCameraID, confID, confValue, isAuto ? POABool.POA_TRUE : POABool.POA_FALSE);
        }

        public static POAErrors POASetConfig(int nCameraID, POAConfig confID, double fValue, bool isAuto) {
            POAValueType pConfValueType;
            POAErrors error = PlayerOnePInvoke.POAGetConfigValueType(confID, out pConfValueType);
            if (error == POAErrors.POA_OK) {
                if (pConfValueType != POAValueType.VAL_FLOAT) {
                    return POAErrors.POA_ERROR_INVALID_CONFIG;
                }
            } else {
                return error;
            }
            POAConfigValue confValue = new POAConfigValue();
            confValue.floatValue = fValue;

            return PlayerOnePInvoke.POASetConfig(nCameraID, confID, confValue, isAuto ? POABool.POA_TRUE : POABool.POA_FALSE);
        }

        public static POAErrors POASetConfig(int nCameraID, POAConfig confID, bool isEnable) {
            POAValueType pConfValueType;
            POAErrors error = PlayerOnePInvoke.POAGetConfigValueType(confID, out pConfValueType);
            if (error == POAErrors.POA_OK) {
                if (pConfValueType != POAValueType.VAL_BOOL) {
                    return POAErrors.POA_ERROR_INVALID_CONFIG;
                }
            } else {
                return error;
            }
            POAConfigValue confValue = new POAConfigValue();
            confValue.boolValue = isEnable ? POABool.POA_TRUE : POABool.POA_FALSE;

            return PlayerOnePInvoke.POASetConfig(nCameraID, confID, confValue, POABool.POA_FALSE);
        }

        //overload POASetConfig end-------

        //overload POAGetConfig begin-------
        public POAErrors POAGetConfig(int nCameraID, POAConfig confID, out int nValue, out bool isAuto) {
            POAConfigValue confValue = new POAConfigValue();
            POABool boolValue;

            POAErrors error = PlayerOnePInvoke.POAGetConfig(nCameraID, confID, out confValue, out boolValue);
            if (error == POAErrors.POA_OK) {
                nValue = confValue.intValue;
                isAuto = boolValue == POABool.POA_TRUE ? true : false;
                return POAErrors.POA_OK;
            } else {
                nValue = 0;
                isAuto = false;
                return error;
            }
        }

        public POAErrors POAGetConfig(int nCameraID, POAConfig confID, out double fValue, out bool isAuto) {
            POAConfigValue confValue = new POAConfigValue();
            POABool boolValue;

            POAErrors error = PlayerOnePInvoke.POAGetConfig(nCameraID, confID, out confValue, out boolValue);
            if (error == POAErrors.POA_OK) {
                fValue = confValue.floatValue;
                isAuto = boolValue == POABool.POA_TRUE ? true : false;
                return POAErrors.POA_OK;
            } else {
                fValue = 0;
                isAuto = false;
                return error;
            }
        }

        public POAErrors POAGetConfig(int nCameraID, POAConfig confID, out bool isEnable) {
            POAConfigValue confValue = new POAConfigValue();
            POABool boolValue;

            POAErrors error = PlayerOnePInvoke.POAGetConfig(nCameraID, confID, out confValue, out boolValue);
            if (error == POAErrors.POA_OK) {
                isEnable = confValue.boolValue == POABool.POA_TRUE ? true : false;
                return POAErrors.POA_OK;
            } else {
                isEnable = false;
                return error;
            }
        }

        [SecurityCritical]
        public POAErrors POAGetSensorModeCount(int nCameraID, out int pModeCount) {
            return PlayerOnePInvoke.POAGetSensorModeCount(nCameraID, out pModeCount);
        }

        [SecurityCritical]
        public POAErrors POAGetSensorMode(int nCameraID, out int pModeIndex) {
            return PlayerOnePInvoke.POAGetSensorMode(nCameraID, out pModeIndex);
        }

        [SecurityCritical]
        public  POAErrors POAGetSensorModeInfo(int nCameraID, int index, out POASensorModeInfo pSenModeInfo) {
            return PlayerOnePInvoke.POAGetSensorModeInfo(nCameraID, index, out pSenModeInfo);
        }

        [SecurityCritical]
        public  POAErrors POASetSensorMode(int nCameraID, int modeIndex) {
            return PlayerOnePInvoke.POASetSensorMode(nCameraID, modeIndex);
        }
    }

    [ExcludeFromCodeCoverage]
    public class PlayerOnePInvoke {
        private const string DLLNAME = "PlayerOneCamera.dll";

        static PlayerOnePInvoke() {
            DllLoader.LoadDll(Path.Combine("PlayerOne", DLLNAME));
        }

        [DllImport(DLLNAME, EntryPoint = "POAGetCameraCount", CallingConvention = CallingConvention.Cdecl)]
        public static extern int POAGetCameraCount();

        [DllImport(DLLNAME, EntryPoint = "POAGetCameraProperties", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetCameraProperties(int nIndex, out POACameraProperties pProp);

        [DllImport(DLLNAME, EntryPoint = "POAGetCameraPropertiesByID", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetCameraPropertiesByID(int nCameraID, out POACameraProperties pProp);

        [DllImport(DLLNAME, EntryPoint = "POAOpenCamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAOpenCamera(int nCameraID);

        [DllImport(DLLNAME, EntryPoint = "POAInitCamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAInitCamera(int nCameraID);

        [DllImport(DLLNAME, EntryPoint = "POACloseCamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POACloseCamera(int nCameraID);

        [DllImport(DLLNAME, EntryPoint = "POAGetConfigsCount", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetConfigsCount(int nCameraID, out int pConfCount);

        [DllImport(DLLNAME, EntryPoint = "POAGetConfigAttributes", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetConfigAttributes(int nCameraID, int nConfIndex, out POAConfigAttributes pConfAttr);

        [DllImport(DLLNAME, EntryPoint = "POAGetConfigAttributesByConfigID", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetConfigAttributesByConfigID(int nCameraID, POAConfig confID, out POAConfigAttributes pConfAttr);

        [DllImport(DLLNAME, EntryPoint = "POASetConfig", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POASetConfig(int nCameraID, POAConfig confID, POAConfigValue confValue, POABool isAuto);

        [DllImport(DLLNAME, EntryPoint = "POAGetConfig", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetConfig(int nCameraID, POAConfig confID, out POAConfigValue confValue, out POABool isAuto);

        [DllImport(DLLNAME, EntryPoint = "POAGetConfigValueType", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetConfigValueType(POAConfig confID, out POAValueType pConfValueType);

        [DllImport(DLLNAME, EntryPoint = "POASetImageStartPos", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POASetImageStartPos(int nCameraID, int startX, int startY);

        [DllImport(DLLNAME, EntryPoint = "POAGetImageStartPos", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetImageStartPos(int nCameraID, out int pStartX, out int pStartY);

        [DllImport(DLLNAME, EntryPoint = "POASetImageSize", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POASetImageSize(int nCameraID, int width, int height);

        [DllImport(DLLNAME, EntryPoint = "POAGetImageSize", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetImageSize(int nCameraID, out int pWidth, out int pHeight);

        [DllImport(DLLNAME, EntryPoint = "POASetImageBin", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POASetImageBin(int nCameraID, int bin);

        [DllImport(DLLNAME, EntryPoint = "POAGetImageBin", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetImageBin(int nCameraID, out int pBin);

        [DllImport(DLLNAME, EntryPoint = "POASetImageFormat", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POASetImageFormat(int nCameraID, POAImgFormat imgFormat);

        [DllImport(DLLNAME, EntryPoint = "POAGetImageFormat", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetImageFormat(int nCameraID, out POAImgFormat pImgFormat);

        [DllImport(DLLNAME, EntryPoint = "POAStartExposure", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAStartExposure(int nCameraID, POABool bSignalFrame);

        [DllImport(DLLNAME, EntryPoint = "POAStopExposure", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAStopExposure(int nCameraID);

        [DllImport(DLLNAME, EntryPoint = "POAGetCameraState", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetCameraState(int nCameraID, out POACameraState pCameraState);

        [DllImport(DLLNAME, EntryPoint = "POAGetImageData", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetImageData(int nCameraID, [Out] ushort[] pBuf, int nBufSize, int nTimeoutms);

        [DllImport(DLLNAME, EntryPoint = "POAGetDroppedImagesCount", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetDroppedImagesCount(int nCameraID, out int pDroppedCount);

        [DllImport(DLLNAME, EntryPoint = "POASetUserCustomID", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POASetUserCustomID(int nCameraID, IntPtr pCustomID, int len);

        [DllImport(DLLNAME, EntryPoint = "POAGetGainOffset", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetGainOffset(int nCameraID, out int pOffsetHighestDR, out int pOffsetUnityGain, out int pGainLowestRN, out int pOffsetLowestRN, out int pHCGain);

        [DllImport(DLLNAME, EntryPoint = "POASetTrgModeEnable", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POASetTrgModeEnable(int nCameraID, POABool enable);

        [DllImport(DLLNAME, EntryPoint = "POAGetErrorString", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr POAGetErrorString(POAErrors err);

        [DllImport(DLLNAME, EntryPoint = "POAGetAPIVersion", CallingConvention = CallingConvention.Cdecl)]
        public static extern int POAGetAPIVersion();

        [DllImport(DLLNAME, EntryPoint = "POAGetSDKVersion", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr POAGetSDKVersion();

        [DllImport(DLLNAME, EntryPoint = "POAImageReady", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAImageReady(int nCameraID, out POABool pIsReady);

        [DllImport(DLLNAME, EntryPoint = "POAGetSensorModeCount", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetSensorModeCount(int nCameraID, out int pModeCount);

        [DllImport(DLLNAME, EntryPoint = "POAGetSensorMode", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetSensorMode(int nCameraID, out int pModeIndex); 

        [DllImport(DLLNAME, EntryPoint = "POAGetSensorModeInfo", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POAGetSensorModeInfo(int nCameraID, int index, out POASensorModeInfo pSenModeInfo);

        [DllImport(DLLNAME, EntryPoint = "POASetSensorMode", CallingConvention = CallingConvention.Cdecl)]
        public static extern POAErrors POASetSensorMode(int nCameraID, int modeIndex);
    }

    public enum POABool // BOOL Value Definition
        {
        POA_FALSE = 0,  // false
        POA_TRUE        // true
    }

    public enum POABayerPattern // Bayer Pattern Definition
    {
        POA_BAYER_RG = 0,   // RGGB
        POA_BAYER_BG,       // BGGR
        POA_BAYER_GR,       // GRBG
        POA_BAYER_GB,       // GBRG
        POA_BAYER_MONO = -1 // Monochrome, the mono camera with this
    }

    public enum POAImgFormat  // Image Data Format Definition
    {
        POA_RAW8 = 0,       // 8bit raw data, 1 pixel 1 byte, value range[0, 255]
        POA_RAW16,      // 16bit raw data, 1 pixel 2 bytes, value range[0, 65535]
        POA_RGB24,      // RGB888 color data, 1 pixel 3 bytes, value range[0, 255] (only color camera)
        POA_MONO8,      // 8bit monochrome data, convert the Bayer Filter Array to monochrome data. 1 pixel 1 byte, value range[0, 255] (only color camera)
        POA_END = -1
    }

    public enum POAErrors                 // Return Error Code Definition
    {
        POA_OK = 0,                         // operation successful
        POA_ERROR_INVALID_INDEX,            // invalid index, means the index is < 0 or >= the count( camera or config)
        POA_ERROR_INVALID_ID,               // invalid camera ID
        POA_ERROR_INVALID_CONFIG,           // invalid POAConfig
        POA_ERROR_INVALID_ARGU,             // invalid argument(parameter)
        POA_ERROR_NOT_OPENED,               // camera not opened
        POA_ERROR_DEVICE_NOT_FOUND,         // camera not found, may be removed
        POA_ERROR_OUT_OF_LIMIT,             // the value out of limit
        POA_ERROR_EXPOSURE_FAILED,          // camera exposure failed
        POA_ERROR_TIMEOUT,                  // timeout
        POA_ERROR_SIZE_LESS,                // the data buffer size is not enough
        POA_ERROR_EXPOSING,                 // camera is exposing. some operation, must stop exposure first
        POA_ERROR_POINTER,                  // invalid pointer, when get some value, do not pass the NULL pointer to the function
        POA_ERROR_CONF_CANNOT_WRITE,        // the POAConfig is not writable
        POA_ERROR_CONF_CANNOT_READ,         // the POAConfig is not readable
        POA_ERROR_ACCESS_DENIED,            // access denied
        POA_ERROR_OPERATION_FAILED,         // operation failed
        POA_ERROR_MEMORY_FAILED             // memory allocation failed
    }

    public enum POACameraState            // Camera State Definition
    {
        STATE_CLOSED = 0,                   // camera was closed
        STATE_OPENED,                       // camera was opened, but not exposing
        STATE_EXPOSING                      // camera is exposing
    }

    public enum POAValueType              // Config Value Type Definition
    {
        VAL_INT = 0,                        // integer(int)
        VAL_FLOAT,                          // float(double)
        VAL_BOOL                            // bool(POABool)
    }

    public enum POAConfig                 // Camera Config Definition
    {
        POA_EXPOSURE = 0,                   // exposure time(unit: us), read-write, valueType == VAL_INT
        POA_GAIN,                           // gain, read-write, valueType == VAL_INT
        POA_HARDWARE_BIN,                   // hardware bin, read-write, valueType == VAL_BOOL
        POA_TEMPERATURE,                    // camera temperature(uint: C), read-only, valueType == VAL_FLOAT
        POA_WB_R,                           // red pixels coefficient of white balance, read-write, valueType == VAL_INT
        POA_WB_G,                           // green pixels coefficient of white balance, read-write, valueType == VAL_INT
        POA_WB_B,                           // blue pixels coefficient of white balance, read-write, valueType == VAL_INT
        POA_OFFSET,                         // camera offset, read-write, valueType == VAL_INT
        POA_AUTOEXPO_MAX_GAIN,              // maximum gain when auto-adjust, read-write, valueType == VAL_INT
        POA_AUTOEXPO_MAX_EXPOSURE,          // maximum exposure when auto-adjust(uint: ms), read-write, valueType == VAL_INT
        POA_AUTOEXPO_BRIGHTNESS,            // target brightness when auto-adjust, read-write, valueType == VAL_INT
        POA_GUIDE_NORTH,                    // ST4 guide north, generally,it's DEC+ on the mount, read-write, valueType == VAL_BOOL
        POA_GUIDE_SOUTH,                    // ST4 guide south, generally,it's DEC- on the mount, read-write, valueType == VAL_BOOL
        POA_GUIDE_EAST,                     // ST4 guide east, generally,it's RA- on the mount, read-write, valueType == VAL_BOOL
        POA_GUIDE_WEST,                     // ST4 guide west, generally,it's RA+ on the mount, read-write, valueType == VAL_BOOL
        POA_EGAIN,                          // e/ADU, This value will change with gain, read-only, valueType == VAL_FLOAT
        POA_COOLER_POWER,                   // cooler power percentage[0-100%](only cool camera), read-only, valueType == VAL_INT
        POA_TARGET_TEMP,                    // camera target temperature(uint: C), read-write, valueType == VAL_INT
        POA_COOLER,                         // turn cooler(and fan) on or off, read-write, valueType == VAL_BOOL
        POA_HEATER,                         // turn lens heater on or off, read-write, valueType == VAL_BOOL
        POA_HEATER_POWER,                   // lens heater power percentage[0-100%], read-write, valueType == VAL_INT
        POA_FAN_POWER,                      // radiator fan power percentage[0-100%], read-write, valueType == VAL_INT
        POA_FLIP_NONE,                      // no flip, Note: set this config(POASetConfig), the 'confValue' will be ignored, read-write, valueType == VAL_BOOL
        POA_FLIP_HORI,                      // flip the image horizontally, Note: set this config(POASetConfig), the 'confValue' will be ignored, read-write, valueType == VAL_BOOL
        POA_FLIP_VERT,                      // flip the image vertically, Note: set this config(POASetConfig), the 'confValue' will be ignored, read-write, valueType == VAL_BOOL
        POA_FLIP_BOTH,                      // flip the image horizontally and vertically, Note: set this config(POASetConfig), the 'confValue' will be ignored, read-write, valueType == VAL_BOOL
        POA_FRAME_LIMIT,                    // Frame rate limit, the range:[0, 2000], 0 means no limit, read-write, valueType == VAL_INT
        POA_HQI,                            // High quality image, for those without DDR camera(guide camera), if set POA_TRUE, this will reduce the waviness and stripe of the image,
                                            // but frame rate may go down, note: this config has no effect on those cameras that with DDR. read-write, valueType == VAL_BOOL
        POA_USB_BANDWIDTH_LIMIT,            // USB bandwidth limit, read-write, valueType == VAL_INT
        POA_PIXEL_BIN_SUM                   // take the sum of pixels after binning, POA_TRUE is sum and POA_FLASE is average, default is POA_FLASE, read-write, valueType == VAL_BOOL
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POACameraProperties     // Camera Properties Definition
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 256)]
        public byte[] cameraName;            // the camera name

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
        public byte[] customID;            // user custom name, it will be will be added after the camera name, max len 16 bytes,like:Mars-C[Juno], default is empty

        public int cameraID;                       // it's unique,camera can be controlled and set by the cameraID
        public int maxWidth;                       // max width of the camera
        public int maxHeight;                      // max height of the camera
        public int bitDepth;                       // ADC depth of image sensor
        public POABool isColorCamera;              // is a color camera or not
        public POABool isHasST4Port;               // does the camera have ST4 port, if not, camera don't support ST4 guide
        public POABool isHasCooler;                // does the camera have cooler, generally, the cool camera with cooler
        public POABool isUSB3Speed;                // is usb3.0 speed
        public POABayerPattern bayerPattern;       // the bayer filter pattern of camera
        public double pixelSize;                   // camera pixel size(unit: um)
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
        public byte[] serialNumber;                        // the serial number of camera,it's unique

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 32)]
        public byte[] sensorName;                        // the sersor model(name) of camera, eg: IMX462

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 256)]
        public byte[] localPathInHost;                // the path of the camera in the computer host

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public int[] bins;                        // bins supported by the camera, 1 == bin1, 2 == bin2,..., end with 0, eg:[1,2,3,4,0,0,0,0]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public POAImgFormat[] imgFormats;         // image data format supported by the camera, end with POA_END, eg:[POA_RAW8, POA_RAW16, POA_END,...]

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 256)]
        public byte[] reserved;                 // reserved

        public string cameraModelName {
            get { return Encoding.ASCII.GetString(cameraName).TrimEnd((Char)0); }
        }

        public string userCustomID {
            get { return Encoding.ASCII.GetString(customID).TrimEnd((Char)0); }
        }

        public string SN {
            get { return Encoding.ASCII.GetString(serialNumber).TrimEnd((Char)0); }
        }

        public string sensorModelName {
            get { return Encoding.ASCII.GetString(sensorName).TrimEnd((Char)0); }
        }

        public string localPath {
            get { return Encoding.ASCII.GetString(localPathInHost).TrimEnd((Char)0); }
        }
    }

    // https://csharppedia.com/en/tutorial/5626/how-to-use-csharp-structs-to-create-a-union-type---similar-to-c-unions-
    //The Struct needs to be annotated as "Explicit Layout", like this:
    [StructLayout(LayoutKind.Explicit)]
    public struct POAConfigValue           // Config Value Definition
    {
        //The "FieldOffset:" means that this Integer starts 
        //Offset 0, in bytes, (with sizeof(long) = 4 bytes length): 
        [FieldOffset(0)]
        public int intValue;                      // int

        //Offset 0, (length sizeof(double) = 8 bytes)...   
        [FieldOffset(0)]
        public double floatValue;                  // double

        //Offset 0, (length sizeof(enum) = 4 bytes)...   
        [FieldOffset(0)]
        public POABool boolValue;                  // POABool
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POAConfigAttributes     // Camera Config Attributes Definition(every POAConfig has a POAConfigAttributes)
    {
        public POABool isSupportAuto;              // is support auto?
        public POABool isWritable;                 // is writable?
        public POABool isReadable;                 // is readable?
        public POAConfig configID;                 // config ID, eg: POA_EXPOSURE
        public POAValueType valueType;             // value type, eg: VAL_INT
        public POAConfigValue maxValue;            // maximum value
        public POAConfigValue minValue;            // minimum value
        public POAConfigValue defaultValue;        // default value
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
        public byte[] configName;                // POAConfig name, eg: POA_EXPOSURE: "Exposure", POA_TARGET_TEMP: "TargetTemp"
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 128)]
        public byte[] configDescription;            // a brief introduction about this one POAConfig

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
        public byte[] reserved;                  // reserved

        public string szConfName {
            get { return Encoding.ASCII.GetString(configName).TrimEnd((Char)0); }
        }

        public string szDescription {
            get { return Encoding.ASCII.GetString(configDescription).TrimEnd((Char)0); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POASensorModeInfo         //The information of sensor mode 
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
        public byte[] modeName;                        //name of sensor mode that can be displayed on the UI, eg: combobox

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 128)]
        public byte[] modeDesc;                        //description of sensor mode, which can be used for tooltips 

        public string name {
            get { return Encoding.ASCII.GetString(modeName).TrimEnd((Char)0); }
        }

        public string desc {
            get { return Encoding.ASCII.GetString(modeDesc).TrimEnd((Char)0); }
        }
    }

}