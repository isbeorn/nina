#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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