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

using FluentAssertions;
using FTD2XX_NET;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.MGEN.Commands {

    public abstract class CommandTestRunner {

        private delegate void MockReadCallback(byte[] x, int y, out uint z);

        private delegate void MockWriteCallback(byte[] x, int y, out uint z);

        private static int reads = 0;
        private static int writes = 0;

        protected void SetupRead(Mock<IFTDI> mock, params object[] list) {
            mock.Setup(m => m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), out It.Ref<uint>.IsAny))
                .Callback(new MockReadCallback((byte[] x, int y, out uint z) => {
                    var setupList = (byte[])list[reads];
                    for (int i = 0; i < x.Length; i++) {
                        if (i < setupList.Length) {
                            x[i] = setupList[i];
                        }
                    }
                    z = (uint)x.Length;
                    reads++;
                }))
                .Returns(FT_STATUS.FT_OK);
        }

        protected void SetupWrite(Mock<IFTDI> mock, params object[] list) {
            mock.Setup(m => m.Write(It.IsAny<byte[]>(), It.IsAny<int>(), out It.Ref<uint>.IsAny))
                .Callback(new MockWriteCallback((byte[] x, int y, out uint z) => {
                    var expectedWrite = (byte[])list[writes];
                    x.Should().BeEquivalentTo(expectedWrite);
                    z = (uint)x.Length;
                    writes++;
                }))
                .Returns(FT_STATUS.FT_OK);
        }

        protected byte[] GetBytes(ushort value) {
            var bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) { Array.Reverse(bytes); }
            return bytes;
        }

        [SetUp]
        public void InitializeTest() {
            reads = 0;
            writes = 0;
        }
    }
}