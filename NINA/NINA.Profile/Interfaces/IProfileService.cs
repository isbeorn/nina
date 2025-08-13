#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Globalization;

namespace NINA.Profile.Interfaces {

    public interface IProfileService {
        bool ProfileWasSpecifiedFromCommandLineArgs { get; }
        AsyncObservableCollection<ProfileMeta> Profiles { get; }
        IProfile ActiveProfile { get; }

        bool Clone(ProfileMeta profileInfos);

        void Add();

        bool SelectProfile(ProfileMeta profileInfo);

        bool RemoveProfile(ProfileMeta profileInfo);

        void ChangeLocale(CultureInfo language);

        void ChangeLatitude(double latitude);

        void ChangeLongitude(double longitude);

        void ChangeElevation(double elevation);

        void ChangeHorizon(string horizonFilePath);

        /// <summary>
        /// Release locks of active profile. Only call prior to shutdown to free ressources
        /// </summary>
        void Release();

        event EventHandler LocaleChanged;

        event EventHandler LocationChanged;

        event EventHandler BeforeProfileChanging;

        event EventHandler ProfileChanged;

        event EventHandler HorizonChanged;
    }
}