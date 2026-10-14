#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Equipment.Interfaces {

    public interface ICompositeSwitchIdentity {
        /// <summary>The original child driver ID, before hub qualification.</summary>
        string DeviceId { get; }

        /// <summary>The child driver's native channel ID.</summary>
        short ChannelId { get; }

        /// <summary>The name used for expression identifiers, independent of the visible label.</summary>
        string ExpressionName => this is ISwitch channel ? channel.Name : string.Empty;
    }
}
