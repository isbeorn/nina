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
using System.Net;
using System.Net.Sockets;

namespace NINA.Core.Utility {
    public class DnsHelper {
        /// <summary>
        /// Returns an IPHostEntry object for the specified host name. If the host name is an IP address, the IPHostEntry object will contain only that IP address.
        /// This is a more robust version of Dns.GetHostEntry that avoids some issues specifically around loopback IP addresses.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="addressFamily"></param>
        /// <returns cref="IPHostEntry"></returns>
        /// <exception cref="ArgumentNullException">If hostname is null or empty.</exception>"
        /// <exception cref="SocketException">If hostname could not be resolved.</exception>"
        /// <exception cref="ArgumentException">If hostname is an invalid IP address</exception>
        public static IPHostEntry GetIPHostEntryByName(string hostName, AddressFamily addressFamily = AddressFamily.InterNetwork) {
            if (string.IsNullOrEmpty(hostName)) {
                throw new ArgumentNullException(nameof(hostName));
            }

            hostName = hostName.Trim();

            // First see if the supplied address is an IP address. Try to resolve the supplied hostname only if it isn't an IP address.
            if (IPAddress.TryParse(hostName, out var ip)) {
                return new IPHostEntry { AddressList = [ ip ] };
            } else {
                return Dns.GetHostEntry(hostName, addressFamily);
            }
        }
    }
}