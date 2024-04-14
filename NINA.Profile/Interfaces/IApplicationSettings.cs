#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NINA.Profile.Interfaces {

    public interface IApplicationSettings : ISettings {
        string Culture { get; set; }
        double DevicePollingInterval { get; set; }
        CultureInfo Language { get; set; }
        LogLevelEnum LogLevel { get; set; }
        [Obsolete]
        string SkyAtlasImageRepository { get; set; }
        string SkySurveyCacheDirectory { get; set; }
        AsyncObservableCollection<KeyValuePair<string, string>> SelectedPluggableBehaviors { get; set; }
        IReadOnlyDictionary<string, string> SelectedPluggableBehaviorsLookup { get; }
        int PageSize { get; set; }
    }
}