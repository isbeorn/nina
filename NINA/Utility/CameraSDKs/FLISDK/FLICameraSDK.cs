#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Copyright (c) 2019 Dale Ghent <daleg@elemental.org> All rights reserved.
 */

#endregion "copyright"

using NINA.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FLI {

    public static class LibFLI {
        private const string DLLNAME = "libfli.dll";

        static LibFLI() {
            DllLoader.LoadDll(Path.Combine("FLI", DLLNAME));
        }

        #region "FLI SDK constants"

        /// <summary>
        /// API
        /// </summary>
        public const uint FLI_SUCCESS = 0;

        /// <summary>
        /// Interface and device type domains
        /// </summary>
        public enum FLIDomains : uint {
            IF_NONE = 0x00,
            IF_PARALLEL_PORT = 0x01,
            IF_USB = 0x02,
            IF_SERIAL = 0x03,
            IF_INET = 0x04,
            IF_SERIAL_19200 = 0x05,
            IF_SERIAL_1200 = 0x06,

            DEV_NONE = 0x000,
            DEV_CAMERA = 0x100,
            DEV_FILTERWHEEL = 0x200,
            DEV_FOCUSER = 0x300,
            DEV_HS_FILTERWHEEL = 0x400,
            DEV_RAW = 0x0f00,
            DEV_ENUMERATE_BY_CONNECTION = 0x8000
        }

        /// <summary>
        /// Frame types
        /// </summary>
        public enum FLIFrameType : uint {
            NORMAL = 0,
            DARK,
            FLOOD,
            RBI_FLUSH = FLOOD | DARK
        }

        /// <summary>
        /// Bit depths
        /// </summary>
        public enum FLIBitDepth : uint {
            DEPTH_8 = 0,
            DEPTH_16
        }

        /// <summary>
        /// Shutter Control
        /// </summary>
        public enum FLIShutter : uint {
            CLOSE = 0x0000,
            OPEN = 0x0001,
            EXTERNAL_TRIGGER = 0x0002,
            EXTERNAL_TRIGGER_LOW = 0x0002,
            EXTERNAL_TRIGGER_HIGH = 0x0004,
            EXTERNAL_EXPOSURE_CONTROL = 0x0008
        }

        /// <summary>
        /// Flush Control
        /// </summary>
        public enum FLIBGFlush : uint {
            START = 0x0000,
            STOP = 0x0001
        }

        /// <summary>
        /// Temperature sensor channel
        /// </summary>
        public enum FLIChannel : uint {
            INTERNAL = 0x0000,
            EXTERNAL = 0x0001,
            CCD = 0x0000,
            BASE = 0x0001
        }

        /// <summary>
        /// Camera status
        /// </summary>
        [Flags]
        public enum CameraStatus : uint {
            UNKNOWN = 0xFFFFFFFF,
            MASK = 0x00000003,
            IDLE = 0x00,
            WAITING_FOR_TRIGGER = 0x01,
            EXPOSING = 0x02,
            READING_CCD = 0x03,
            DATA_READY = 0x80000000
        }

        /// <summary>
        /// Focuser status
        /// </summary>
        [Flags]
        public enum FocuserStatus : uint {
            UNKNOWN = 0xFFFFFFFF,
            HOMNIG = 0x00000004,
            MOVING_IN = 0x00000001,
            MOVING_OUT = 0x00000002,
            MOVING_MASK = 0x00000007,
            HOME = 0x00000080,
            LIMIT = 0x00000040,
            LEGACY = 0x10000000
        }

        /// <summary>
        /// Filter Wheel status
        /// </summary>
        [Flags]
        public enum FilterWheelStatus : uint {
            VIRTUAL = 0x0,
            PHYSICAL = 0x100,
            LEFT = PHYSICAL | 0x00,
            RIGHT = PHYSICAL | 0x01,
            MOVING_CCW = 0x01,
            MOVIN_CW = 0x02,
            POSITION_UNKNOWN = 0xff,
            POSITION_CURRENT = 0x200,
            HOMING = 0x00000004,
            HOME = 0x00000080,
            HOME_LEFT = 0x00000080,
            HOME_RIGHT = 0x00000040,
            HOME_SUCCEEDED = 0x00000008
        }

        /// <summary>
        /// FLI SDK debug level
        /// </summary>
        public enum FLIDebugLevel : uint {

            /// <summary>
            /// No messages
            /// </summary>
            NONE = 0x00,

            /// <summary>
            /// Informational messages
            /// </summary>
            INFO = 0x01,

            /// <summary>
            /// Warnings
            /// </summary>
            WARN = 0x02,

            /// <summary>
            /// Failures
            /// </summary>
            FAIL = 0x04,

            /// <summary>
            /// I/O errors
            /// </summary>
            IO = 0x08,

            /// <summary>
            /// ALL
            /// </summary>
            ALL = INFO | WARN | FAIL
        };

        #endregion "FLI SDK constants"

        #region "FLI SDK prototypes"

        [DllImport(DLLNAME, EntryPoint = "FLIOpen", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIOpen(out uint dev, string name, FLIDomains domain);

        [DllImport(DLLNAME, EntryPoint = "FLISetDebugLevel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetDebugLevel(StringBuilder host, uint level);

        [DllImport(DLLNAME, EntryPoint = "FLIClose", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIClose(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLIGetLibVersion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetLibVersion(StringBuilder version, int length);

        [DllImport(DLLNAME, EntryPoint = "FLIGetModel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetModel(uint dev, ref double model, int length);

        [DllImport(DLLNAME, EntryPoint = "FLIGetPixelSize", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetPixelSize(uint dev, ref double pixel_x, ref double pixel_y);

        [DllImport(DLLNAME, EntryPoint = "FLIGetHWRevision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetHWRevision(uint dev, out uint hwrev);

        [DllImport(DLLNAME, EntryPoint = "FLIGetFWRevision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetFWRevision(uint dev, out uint fwrev);

        [DllImport(DLLNAME, EntryPoint = "FLIGetArrayArea", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetArrayArea(uint dev, ref uint ul_x, ref uint ul_y, ref uint lr_x, ref uint lr_y);

        [DllImport(DLLNAME, EntryPoint = "FLIGetVisibleArea", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetVisibleArea(uint dev, ref uint ul_x, ref uint ul_y, ref uint lr_x, ref uint lr_y);

        [DllImport(DLLNAME, EntryPoint = "FLISetExposureTime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetExposureTime(uint dev, uint exptime);

        [DllImport(DLLNAME, EntryPoint = "FLISetImageArea", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetImageArea(uint dev, uint ul_x, uint ul_y, uint lr_x, uint lr_y);

        [DllImport(DLLNAME, EntryPoint = "FLISetHBin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetHBin(uint dev, uint hbin);

        [DllImport(DLLNAME, EntryPoint = "FLISetVBin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetVBin(uint dev, uint vbin);

        [DllImport(DLLNAME, EntryPoint = "FLISetFrameType", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetFrameType(uint dev, uint frametype);

        [DllImport(DLLNAME, EntryPoint = "FLICancelExposure", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLICancelExposure(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLIGetExposureStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetExposureStatus(uint dev, ref uint timeleft);

        [DllImport(DLLNAME, EntryPoint = "FLISetTemperature", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetTemperature(uint dev, double temperature);

        [DllImport(DLLNAME, EntryPoint = "FLIGetTemperature", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetTemperature(uint dev, ref double temperaure);

        [DllImport(DLLNAME, EntryPoint = "FLIGetCoolerPower", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetCoolerPower(uint dev, ref double power);

        [DllImport(DLLNAME, EntryPoint = "FLIGrabRow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGrabRow(uint dev, [Out] ushort[] buff, int width);

        [DllImport(DLLNAME, EntryPoint = "FLIExposeFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIExposeFrame(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLIFlushRow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIFlushRow(uint dev, uint rows, uint repeat);

        [DllImport(DLLNAME, EntryPoint = "FLISetNFlushes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetNFlushes(uint dev, uint nflushes);

        [DllImport(DLLNAME, EntryPoint = "FLISetBitDepth", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetBitDepth(uint dev, uint bitdepth);

        [DllImport(DLLNAME, EntryPoint = "FLIReadIOPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIReadIOPort(uint dev, ref uint ioportset);

        [DllImport(DLLNAME, EntryPoint = "FLIWriteIOPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIWriteIOPort(uint dev, uint ioportset);

        [DllImport(DLLNAME, EntryPoint = "FLIConfigureIOPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIConfigureIOPort(uint dev, uint ioportset);

        [DllImport(DLLNAME, EntryPoint = "FLILockDevice", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLILockDevice(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLIUnlockDevice", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIUnlockDevice(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLIControlShutter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIControlShutter(uint dev, uint shutter);

        [DllImport(DLLNAME, EntryPoint = "FLIControlBackgroundFlush", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIControlBackgroundFlush(uint dev, FLIBGFlush bgflush);

        [DllImport(DLLNAME, EntryPoint = "FLISetDAC", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetDAC(uint dev, uint dacset);

        [DllImport(DLLNAME, EntryPoint = "FLIList", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIList(uint domain, out IntPtr names);

        [DllImport(DLLNAME, EntryPoint = "FLIFreeList", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIFreeList(IntPtr names);

        [DllImport(DLLNAME, EntryPoint = "FLIGetFilterName", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetFilterName(uint dev, uint filter, StringBuilder name, int len);

        [DllImport(DLLNAME, EntryPoint = "FLISetActiveWheel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetActiveWheel(uint dev, uint wheel);

        [DllImport(DLLNAME, EntryPoint = "FLIGetActiveWheel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetActiveWheel(uint dev, ref uint wheel);

        [DllImport(DLLNAME, EntryPoint = "FLISetFilterPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetFilterPos(uint dev, uint filter);

        [DllImport(DLLNAME, EntryPoint = "FLIGetFilterPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetFilterPos(uint dev, ref uint filter);

        [DllImport(DLLNAME, EntryPoint = "FLIGetFilterCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetFilterCount(uint dev, ref uint filter);

        [DllImport(DLLNAME, EntryPoint = "FLIStepMotor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIStepMotor(uint dev, uint steps);

        [DllImport(DLLNAME, EntryPoint = "FLIStepMotorAsync", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIStepMotorAsync(uint dev, uint steps);

        [DllImport(DLLNAME, EntryPoint = "FLIGetStepperPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetStepperPosition(uint dev, ref uint position);

        [DllImport(DLLNAME, EntryPoint = "FLIGetStepsRemaining", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetStepsRemaining(uint dev, ref uint steps);

        [DllImport(DLLNAME, EntryPoint = "FLIHomeFocuser", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIHomeFocuser(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLICreateList", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLICreateList(uint domain);

        [DllImport(DLLNAME, EntryPoint = "FLIDeleteList", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIDeleteList();

        [DllImport(DLLNAME, EntryPoint = "FLIListFirst", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIListFirst(uint domain, uint steps);

        [DllImport(DLLNAME, EntryPoint = "FLIListNext", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIListNext(uint dev, uint steps);

        [DllImport(DLLNAME, EntryPoint = "FLIReadTemperature", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIReadTemperature(uint dev, uint channel, ref double temperature);

        [DllImport(DLLNAME, EntryPoint = "FLIGetFocuserExtent", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetFocuserExtent(uint dev, ref uint extent);

        [DllImport(DLLNAME, EntryPoint = "FLIUsbBulkIO", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIUsbBulkIO(uint dev, ref byte[] buf, ref int len);

        [DllImport(DLLNAME, EntryPoint = "FLIGetDeviceStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetDeviceStatus(uint dev, ref uint status);

        [DllImport(DLLNAME, EntryPoint = "FLIGetCameraModeString", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetCameraModeString(uint dev, uint mode_index, StringBuilder mode_string, int siz);

        [DllImport(DLLNAME, EntryPoint = "FLIGetCameraMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetCameraMode(uint dev, ref uint mode_index);

        [DllImport(DLLNAME, EntryPoint = "FLISetCameraMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetCameraMode(uint dev, uint mode_index);

        [DllImport(DLLNAME, EntryPoint = "FLIHomeDevice", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIHomeDevice(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLIGrabFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGrabFrame(uint dev, byte[] buff, ref int buffsize, ref int bytesgrabbed);

        [DllImport(DLLNAME, EntryPoint = "FLISetTDI", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetTDI(uint dev, uint tdi_rate, uint flags);

        [DllImport(DLLNAME, EntryPoint = "FLIGrabVideoFrame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGrabVideoFrame(uint dev, byte[] buff, int size);

        [DllImport(DLLNAME, EntryPoint = "FLIStopVideoMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIStopVideoMode(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLIStartVideoMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIStartVideoMode(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLIGetSerialString", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetSerialString(uint dev, StringBuilder serial, int len);

        [DllImport(DLLNAME, EntryPoint = "FLIEndExposure", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIEndExposure(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLITriggerExposure", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLITriggerExposure(uint dev);

        [DllImport(DLLNAME, EntryPoint = "FLISetFanSpeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetFanSpeed(uint dev, uint fan_speed);

        [DllImport(DLLNAME, EntryPoint = "FLISetVerticalTableEntry", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLISetVerticalTableEntry(uint dev, uint index, uint height, uint bin, uint mode);

        [DllImport(DLLNAME, EntryPoint = "FLIGetVerticalTableEntry", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetVerticalTableEntry(uint dev, uint index, ref uint height, ref uint bin, ref uint mode);

        [DllImport(DLLNAME, EntryPoint = "FLIGetReadoutDimensions", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIGetReadoutDimensions(uint dev, ref uint width, ref uint hoffset, ref uint hbin, ref uint height, ref uint voffset, ref uint vbin);

        [DllImport(DLLNAME, EntryPoint = "FLIEnableVerticalTable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIEnableVerticalTable(uint dev, uint width, uint offset, uint flags);

        [DllImport(DLLNAME, EntryPoint = "FLIReadUserEEPROM", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIReadUserEEPROM(uint dev, uint loc, uint address, uint length, ref byte[] rbuf);

        [DllImport(DLLNAME, EntryPoint = "FLIWriteUserEEPROM", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint FLIWriteUserEEPROM(uint dev, uint loc, uint address, uint length, byte[] wbuf);

        public static List<string> N_FLIList(uint domain) {
            List<string> cameralist = new List<string>();
            IntPtr p1;
            string s;
            Int64 n;

            CheckReturn(FLIList(domain, out IntPtr names), MethodBase.GetCurrentMethod(), domain, new object[] { names });

            unsafe {
                if (names != IntPtr.Zero) {
                    for (int i = 0; i < 64; i++) {
                        p1 = *((IntPtr*)names.ToPointer());
                        s = Marshal.PtrToStringAnsi(p1);

                        if (string.IsNullOrEmpty(s)) {
                            break;
                        }

                        cameralist.Add(s);

                        n = names.ToInt64() + Marshal.SizeOf(typeof(IntPtr));
                        names = (IntPtr)n;
                    }
                }
            }

            FLIFreeList(names);

            return cameralist;
        }

        #endregion "FLI SDK prototypes"

        /// <summary>
        /// Structure for keeping camera instance information in NINA.
        /// </summary>
        public struct FLICameraInfo {

            /// <summary>
            /// Curren camera bin on X
            /// </summary>
            public short BinX;

            /// <summary>
            /// Curren camera bin on y
            /// </summary>
            public short BinY;

            /// <summary>
            /// Pixel bit depth
            /// </summary>
            public uint Bpp;

            /// <summary>
            /// TEC on/off status
            /// </summary>
            public bool CoolerOn;

            /// <summary>
            /// Desired TEC set point (Celcius)
            /// </summary>
            public double CoolerTargetTemp;

            /// <summary>
            /// Current exposure length in microseconds
            /// </summary>
            public uint ExposureLength;

            /// <summary>
            /// Exposure's origin pixel (X axis coord)
            /// </summary>
            public uint ExposureOriginPixelX;

            /// <summary>
            /// Exposure's origin pixel (Y axis coord)
            /// </summary>
            public uint ExposureOriginPixelY;

            /// <summary>
            /// Exposure's lower-right pixel (X axis coord)
            /// </summary>
            public uint ExposureEndPixelX;

            /// <summary>
            /// Exposure's lower-right pixel (Y axis coord)
            /// </summary>
            public uint ExposureEndPixelY;

            /// <summary>
            /// Exposure's total width
            /// </summary>
            public uint ExposureWidth;

            /// <summary>
            /// Exposure's total height
            /// </summary>
            public uint ExposureHeight;

            /// <summary>
            /// Device hardware revision
            /// </summary>
            public uint HWrev;

            /// <summary>
            /// Current frame type
            /// </summary>
            public FLIFrameType FrameType;

            /// <summary>
            /// Device firmware revision
            /// </summary>
            public uint FWrev;

            /// <summary>
            /// The camera's ID
            /// </summary>
            public string Id;

            /// <summary>
            /// Image width (pixels)
            /// </summary>
            public int ImageX;

            /// <summary>
            /// Image height (pixels)
            /// </summary>
            public int ImageY;

            /// <summary>
            /// Camera index number
            /// </summary>
            public uint Index;

            /// <summary>
            /// The camera's model name
            /// </summary>
            public string Model;

            /// <summary>
            /// Physical pixel width (microns)
            /// </summary>
            public double PixelWidthX;

            /// <summary>
            /// Physical pixel height (microns)
            /// </summary>
            public double PixelWidthY;

            /// <summary>
            /// List of readout mode names
            /// </summary>
            public IList<string> ReadoutModes;

            /// <summary>
            /// Index of readout mode for sequences
            /// </summary>
            public short ReadoutModeNormal;

            /// <summary>
            /// Index of readout mode for single images
            /// </summary>
            public short ReadoutModeSnap;

            /// <summary>
            /// List of support bin modes
            /// </summary>
            public List<int> SupportedBins;

            /// <summary>
            /// Device serial number
            /// </summary>
            public string Serial;
        };

        /// <summary>
        /// Structure for keeping filter wheel instance information in NINA.
        /// </summary>
        public struct FLIFilterWheelInfo {

            /// <summary>
            /// Device hardware revision
            /// </summary>
            public uint HWrev;

            /// <summary>
            /// Device firmware revision
            /// </summary>
            public uint FWrev;

            /// <summary>
            /// The camera's ID
            /// </summary>
            public string Id;

            /// <summary>
            /// Camera index number
            /// </summary>
            public uint Index;

            /// <summary>
            /// The camera's model name
            /// </summary>
            public string Model;

            /// <summary>
            /// Number of filter wheel positions
            /// </summary>
            public uint Positions;
        }

        private static void CheckReturn(uint code, MethodBase callingMethod, params object[] parameters) {
            if (code != 0) {
                throw new FLICameraException("FLI SDK returned an error status", callingMethod, code, parameters);
            }
        }

        public class FLICameraException : Exception {

            public FLICameraException(string message, MethodBase callingMethod, uint code, object[] parameters) : base(CreateMessage(message, callingMethod, code, parameters)) {
            }

            private static string CreateMessage(string message, MethodBase callingMethod, uint code, object[] parameters) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error '" + message + "' from call to ");
                sb.Append("FLI." + callingMethod.Name + "(");
                var paramNames = callingMethod.GetParameters().Select(x => x.Name);
                foreach (var line in paramNames.Zip(parameters, (s, o) => string.Format("{0}={1}, ", s, o))) {
                    sb.Append(line);
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(") Returned error code: " + code);
                return sb.ToString();
            }
        }
    }
}