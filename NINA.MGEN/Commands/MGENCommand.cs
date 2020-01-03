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

using FTD2XX_NET;
using NINA.MGEN.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.MGEN.Commands {

    public abstract class MGENCommand<TResult> : IMGENCommand<TResult> where TResult : IMGENResult {
        public abstract uint RequiredBaudRate { get; }
        public uint Timeout { get; } = 1000;
        public abstract byte CommandCode { get; }
        public abstract byte AcknowledgeCode { get; }

        public abstract TResult Execute(IFTDI device);

        protected void Write(IFTDI device, byte[] data) {
            ValidateBaudRate(device);
            var status = device.Write(data, data.Length, out var writtenBytes);
            if (status != FT_STATUS.FT_OK) {
                throw new FTDIWriteException();
            }
        }

        protected void Write(IFTDI device, byte data) {
            ValidateBaudRate(device);
            var command = new byte[] { data };
            var status = device.Write(command, command.Length, out var writtenBytes);
            if (status != FT_STATUS.FT_OK) {
                throw new FTDIWriteException();
            }
        }

        protected byte[] Read(IFTDI device, int length) {
            ValidateBaudRate(device);

            byte[] buffer = new byte[length];
            var status = device.Read(buffer, buffer.Length, out var readBytes);
            if (status != FT_STATUS.FT_OK) {
                throw new FTDIReadException();
            }
            return buffer;
        }

        private void ValidateBaudRate(IFTDI device) {
            device.SetBaudRate(RequiredBaudRate);
        }

        protected short ToShort(byte first, byte second) {
            var prepared = new byte[] { first, second };
            if (!BitConverter.IsLittleEndian) { Array.Reverse(prepared); }
            return BitConverter.ToInt16(prepared, 0);
        }

        protected ushort ToUShort(byte first, byte second) {
            var prepared = new byte[] { first, second };
            if (!BitConverter.IsLittleEndian) { Array.Reverse(prepared); }
            return BitConverter.ToUInt16(prepared, 0);
        }

        protected int ThreeBytesToInt(byte first, byte second, byte third) {
            var isNegative = (third >> 7) == 1;
            var prepared = new byte[] { first, second, third, isNegative ? (byte)0xff : (byte)0x00 };
            if (!BitConverter.IsLittleEndian) { Array.Reverse(prepared); }
            return BitConverter.ToInt32(prepared, 0);
        }

        protected byte[] GetBytes(ushort value) {
            var bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) { Array.Reverse(bytes); }
            return bytes;
        }
    }
}