#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace FTD2XX_NET {

    public interface IFTDI {
        bool IsOpen { get; }

        FT_STATUS Close();

        FT_STATUS GetDeviceList(out FT_DEVICE_INFO_NODE[] devicelist);

        FT_STATUS GetDriverVersion(out uint driverVersion);

        FT_STATUS Open(FT_DEVICE_INFO_NODE device);

        FT_STATUS Read(byte[] dataBuffer, uint bytesToRead, out uint bytesRead);

        FT_STATUS Read(byte[] dataBuffer, int bytesToRead, out uint bytesRead);

        FT_STATUS SetBaudRate(uint baudRate);

        FT_STATUS SetBitMode(byte mask, byte bitMode);

        FT_STATUS SetTimeouts(uint readTimeout, uint writeTimeout);

        FT_STATUS Write(byte[] dataBuffer, uint bytesToWrite, out uint bytesWritten);

        FT_STATUS Write(byte[] dataBuffer, int bytesToWrite, out uint bytesWritten);
    }
}
