#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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