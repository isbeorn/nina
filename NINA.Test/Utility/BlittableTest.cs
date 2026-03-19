#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Utility;
using System.Drawing;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using NUnit.Framework.Legacy;

namespace NINA.Test.Utility {

    [TestFixture]
    public class BlittableTest {

        [Test]
        public void Primitives_Blittable() {
            Assert.That(Blittable<int>.IsBlittable, Is.True);
            Assert.That(Blittable<uint>.IsBlittable, Is.True);
            Assert.That(Blittable<float>.IsBlittable, Is.True);
            Assert.That(Blittable<bool>.IsBlittable, Is.True);
        }

        [Test]
        public void Array_of_Primitives_Blittable() {
            Assert.That(Blittable<int[]>.IsBlittable, Is.True);
            Assert.That(Blittable<uint[]>.IsBlittable, Is.True);
            Assert.That(Blittable<float[]>.IsBlittable, Is.True);
            Assert.That(Blittable<bool[]>.IsBlittable, Is.True);
        }

        [Test]
        public void Struct_and_Class_with_layout_Blittable() {
            // Both classes and structs (reference and value) are blittable if they are laid out property, with StructLayout if necessary
            Assert.That(Blittable<Point>.IsBlittable, Is.True);
            Assert.That(Blittable<SBIG.QueryCommandStatusParams>.IsBlittable, Is.True);
        }

        [Test]
        public void Array_of_Struct_with_layout_Blittable() {
            // An array of blittable value types is also blittable
            Assert.That(Blittable<Point[]>.IsBlittable, Is.True);
        }

        [Test]
        public void Array_of_Class_with_layout_Not_Blittable() {
            // However, an array of reference types is not blittable, even if the element type is
            Assert.That(Blittable<SBIG.QueryCommandStatusParams[]>.IsBlittable, Is.False);
        }

        [Test]
        public void Managed_Class_Not_Blittable() {
            Assert.That(Blittable<SBIG.FailedOperation>.IsBlittable, Is.False);
        }
    }
}