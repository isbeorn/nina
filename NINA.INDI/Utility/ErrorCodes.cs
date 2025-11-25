#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NINA.INDI.Utility {
    public static class ErrorCodes {
        public static readonly int NotImplemented = unchecked((int)0x80040400);
        public static readonly int InvalidValue = unchecked((int)0x80040401);
        public static readonly int ValueNotSet = unchecked((int)0x80040402);
        public static readonly int NotConnected = unchecked((int)0x80040407);
        public static readonly int InvalidWhileParked = unchecked((int)0x80040408);
        public static readonly int InvalidWhileSlaved = unchecked((int)0x80040409);
        public static readonly int SettingsProviderError = unchecked((int)0x8004040A);
        public static readonly int InvalidOperationException = unchecked((int)0x8004040B);
        public static readonly int ActionNotImplementedException = unchecked((int)0x8004040C);
        public static readonly int NotInCacheException = unchecked((int)0x8004040D);
        public static readonly int OperationCancelled = unchecked((int)0x8004040E);
        public static readonly int UnspecifiedError = unchecked((int)0x800404FF);
        public static readonly int DriverBase = unchecked((int)0x80040500);
        public static readonly int DriverMax = unchecked((int)0x80040FFF);

        private static Dictionary<int, string> indiExceptions = new Dictionary<int, string>
        {
            { NotImplemented, "NotImplementedException" },
            { InvalidValue, "InvalidValueException" },
            { ValueNotSet, "ValueNotSetException" },
            { NotConnected, "NotConnectedException" },
            { InvalidWhileParked, "InvalidWhileParkedException" },
            { InvalidWhileSlaved, "InvalidWhileSlavedException" },
            { SettingsProviderError, "SettingsProviderErrorException" },
            { InvalidOperationException, "InvalidOperationExceptionException" },
            { ActionNotImplementedException, "ActionNotImplementedExceptionException" },
            { NotInCacheException, "NotInCacheExceptionException" },
            { OperationCancelled, "OperationCancelledException" },
            { UnspecifiedError, "UnspecifiedErrorException" }
        };

        public static string GetExceptionName(Exception exception) {
            if (exception is COMException ex) {
                try {
                    return indiExceptions[ex.ErrorCode];
                } catch (Exception) {
                    return null;
                }
            }

            return null;
        }
    }
}
