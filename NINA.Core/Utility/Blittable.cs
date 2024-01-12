#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Utility {

    /// <summary>
    /// Blittable types can be natively represented on the machine hardware, so no marshaling is required to P/Invoke native code.
    /// This class provides a convenience mechanism to cache and return the blittability of a type
    ///
    /// Usage:
    ///   Blittable<int>.IsBlittable (true)
    ///   Blittable<Point>.IsBlittable (false)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Blittable<T> {
        public static readonly bool IsBlittable = IsBlittableImpl(typeof(T));

        private static bool IsBlittableImpl(Type type) {
            if (type.IsArray) {
                var elem = type.GetElementType();
                return elem.IsValueType && IsBlittableImpl(elem);
            }
            try {
                object instance = FormatterServices.GetUninitializedObject(type);
                GCHandle.Alloc(instance, GCHandleType.Pinned).Free();
                return true;
            } catch (Exception) {
                return false;
            }
        }
    }
}