#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

namespace FTD2XX_NET {

    // Constants for FT_STATUS
    /// <summary>
    /// Status values for FTDI devices.
    /// </summary>
    public enum FT_STATUS {

        /// <summary>
        /// Status OK
        /// </summary>
        FT_OK = 0,

        /// <summary>
        /// The device handle is invalid
        /// </summary>
        FT_INVALID_HANDLE,

        /// <summary>
        /// Device not found
        /// </summary>
        FT_DEVICE_NOT_FOUND,

        /// <summary>
        /// Device is not open
        /// </summary>
        FT_DEVICE_NOT_OPENED,

        /// <summary>
        /// IO error
        /// </summary>
        FT_IO_ERROR,

        /// <summary>
        /// Insufficient resources
        /// </summary>
        FT_INSUFFICIENT_RESOURCES,

        /// <summary>
        /// A parameter was invalid
        /// </summary>
        FT_INVALID_PARAMETER,

        /// <summary>
        /// The requested baud rate is invalid
        /// </summary>
        FT_INVALID_BAUD_RATE,

        /// <summary>
        /// Device not opened for erase
        /// </summary>
        FT_DEVICE_NOT_OPENED_FOR_ERASE,

        /// <summary>
        /// Device not poened for write
        /// </summary>
        FT_DEVICE_NOT_OPENED_FOR_WRITE,

        /// <summary>
        /// Failed to write to device
        /// </summary>
        FT_FAILED_TO_WRITE_DEVICE,

        /// <summary>
        /// Failed to read the device EEPROM
        /// </summary>
        FT_EEPROM_READ_FAILED,

        /// <summary>
        /// Failed to write the device EEPROM
        /// </summary>
        FT_EEPROM_WRITE_FAILED,

        /// <summary>
        /// Failed to erase the device EEPROM
        /// </summary>
        FT_EEPROM_ERASE_FAILED,

        /// <summary>
        /// An EEPROM is not fitted to the device
        /// </summary>
        FT_EEPROM_NOT_PRESENT,

        /// <summary>
        /// Device EEPROM is blank
        /// </summary>
        FT_EEPROM_NOT_PROGRAMMED,

        /// <summary>
        /// Invalid arguments
        /// </summary>
        FT_INVALID_ARGS,

        /// <summary>
        /// An other error has occurred
        /// </summary>
        FT_OTHER_ERROR
    };
}