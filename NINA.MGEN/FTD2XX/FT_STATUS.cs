#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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