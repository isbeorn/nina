#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.MGEN;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FTD2XX_NET {

    /// <summary>
    /// This is a custom C# implementation for the FTD2XX dll. A detailed reference guide can be found at
    /// https://www.ftdichip.com/Support/Documents/ProgramGuides/D2XX_Programmer's_Guide(FT_000071).pdf
    /// </summary>
    internal partial class FTD2XX : IFTDI {
        private const string DLLNAME = "ftd2xx.dll";
        private IntPtr handle;
        private static object lockObj = new object();

        public FTD2XX(string ftdiDllPath) {
            DllLoader.LoadDll(ftdiDllPath);
        }

        public bool IsOpen => handle != IntPtr.Zero;

        [DllImport(DLLNAME, EntryPoint = "FT_Close", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_Close(IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "FT_GetDriverVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_GetDriverVersion(IntPtr handle, out uint driverVersion);

        [DllImport(DLLNAME, EntryPoint = "FT_CreateDeviceInfoList", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_CreateDeviceInfoList(out ulong nrOfDevices);

        [DllImport(DLLNAME, EntryPoint = "FT_GetDeviceInfoDetail", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_GetDeviceInfoDetail(uint index, out uint flags, out FT_DEVICE ft_device, out uint id, out uint locationId, [Out] byte[] serialNo, [Out] byte[] description, out IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "FT_OpenEx", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_OpenEx(string serialNo, uint mode, out IntPtr handle);

        [DllImport(DLLNAME, EntryPoint = "FT_SetDataCharacteristics", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_SetDataCharacteristics(IntPtr handle, byte wordLength, byte stopBits, byte parity);

        [DllImport(DLLNAME, EntryPoint = "FT_SetFlowControl", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_SetFlowControl(IntPtr handle, ushort flowControl, byte Xon, byte Xoff);

        [DllImport(DLLNAME, EntryPoint = "FT_SetBaudRate", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_SetBaudRate(IntPtr handle, uint baudRate);

        [DllImport(DLLNAME, EntryPoint = "FT_Read", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_Read(IntPtr handle, byte[] buffer, uint bytesToRead, out uint bytesRead);

        [DllImport(DLLNAME, EntryPoint = "FT_SetBitMode", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_SetBitMode(IntPtr handle, byte mask, byte bitMode);

        [DllImport(DLLNAME, EntryPoint = "FT_SetTimeouts", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_SetTimeouts(IntPtr handle, uint readTimeout, uint writeTimeout);

        [DllImport(DLLNAME, EntryPoint = "FT_Write", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_STATUS FT_Write(IntPtr handle, byte[] buffer, uint bytesToWrite, out uint bytesWritten);

        public FT_STATUS GetDriverVersion(out uint driverVersion) {
            return FT_GetDriverVersion(this.handle, out driverVersion);
        }

        public FT_STATUS Close() {
            lock (lockObj) {
                var status = FT_Close(this.handle);
                this.handle = IntPtr.Zero;
                return status;
            }
        }

        public FT_STATUS GetDeviceList(out FT_DEVICE_INFO_NODE[] devicelist) {
            lock (lockObj) {
                var status = FT_CreateDeviceInfoList(out var deviceCount);
                devicelist = new FT_DEVICE_INFO_NODE[deviceCount];
                for (uint i = 0; i < (uint)deviceCount; i++) {
                    byte[] serialNoBytes = new byte[16];
                    byte[] descriptionBytes = new byte[64];
                    status = FT_GetDeviceInfoDetail(i, out var flags, out var device, out var id, out var locationId, serialNoBytes, descriptionBytes, out var handle);
                    var serialNo = Encoding.ASCII.GetString(serialNoBytes);
                    var nullIdx = serialNo.IndexOf("\0");
                    if (nullIdx > -1) {
                        serialNo = serialNo.Substring(0, nullIdx);
                    }
                    var description = Encoding.ASCII.GetString(descriptionBytes);
                    nullIdx = description.IndexOf("\0");
                    if (nullIdx > -1) {
                        description = description.Substring(0, nullIdx);
                    }
                    devicelist[i] = new FT_DEVICE_INFO_NODE() {
                        ID = id,
                        Flags = flags,
                        ftHandle = handle,
                        SerialNumber = serialNo.Trim(),
                        LocId = locationId,
                        Description = description.Trim(),
                        Type = device
                    };
                }
                return status;
            }
        }

        public FT_STATUS Open(FT_DEVICE_INFO_NODE device) {
            lock (lockObj) {
                if (device.ftHandle != IntPtr.Zero) {
                    this.lastBaudRate = 0;
                    this.handle = device.ftHandle;
                    return FT_STATUS.FT_OK;
                }

                var status = FT_OpenEx(device.SerialNumber, 0x00000001, out this.handle);
                if (status == FT_STATUS.FT_OK) {
                    // Initialise port data characteristics
                    var wordLength = FT_DATA_BITS.FT_BITS_8;
                    var stopBits = FT_STOP_BITS.FT_STOP_BITS_1;
                    var parity = FT_PARITY.FT_PARITY_NONE;
                    status = FT_SetDataCharacteristics(handle, wordLength, stopBits, parity);

                    // Initialise to no flow control
                    var flowControl = FT_FLOW_CONTROL.FT_FLOW_NONE;
                    byte Xon = 0x11;
                    byte Xoff = 0x13;
                    status = FT_SetFlowControl(handle, flowControl, Xon, Xoff);

                    // Initialise Baud rate
                    uint baudRate = 9600;
                    status = FT_SetBaudRate(handle, baudRate);
                }
                return status;
            }
        }

        public FT_STATUS Read(byte[] dataBuffer, int bytesToRead, out uint bytesRead) {
            return Read(dataBuffer, (uint)bytesToRead, out bytesRead);
        }

        public FT_STATUS Read(byte[] dataBuffer, uint bytesToRead, out uint bytesRead) {
            lock (lockObj) {
                return FT_Read(this.handle, dataBuffer, bytesToRead, out bytesRead);
            }
        }

        private uint lastBaudRate;

        public FT_STATUS SetBaudRate(uint baudRate) {
            lock (lockObj) {
                if (lastBaudRate != baudRate) {
                    var status = FT_SetBaudRate(handle, baudRate);
                    if (status == FT_STATUS.FT_OK) {
                        lastBaudRate = baudRate;
                    }
                    return status;
                } else {
                    return FT_STATUS.FT_OK;
                }
            }
        }

        public FT_STATUS SetBitMode(byte mask, byte bitMode) {
            lock (lockObj) {
                return FT_SetBitMode(this.handle, mask, bitMode);
            }
        }

        public FT_STATUS SetTimeouts(uint readTimeout, uint writeTimeout) {
            lock (lockObj) {
                return FT_SetTimeouts(this.handle, readTimeout, writeTimeout);
            }
        }

        public FT_STATUS Write(byte[] dataBuffer, int bytesToWrite, out uint bytesWritten) {
            return Write(dataBuffer, (uint)bytesToWrite, out bytesWritten);
        }

        public FT_STATUS Write(byte[] dataBuffer, uint bytesToWrite, out uint bytesWritten) {
            lock (lockObj) {
                return FT_Write(this.handle, dataBuffer, bytesToWrite, out bytesWritten);
            }
        }
    }
}