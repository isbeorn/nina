#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.ComponentModel;
using NINA.Astrometry;
using NINA.Core.Interfaces;
using NINA.CustomControlLibrary;
using Nito.Mvvm;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IDeepSkyObjectSearchVM : INotifyPropertyChanged {
        Coordinates Coordinates { get; set; }
        int Limit { get; set; }
        IAutoCompleteItem SelectedTargetSearchResult { get; set; }
        bool ShowPopup { get; set; }
        string TargetName { get; set; }
        NotifyTask<List<IAutoCompleteItem>> TargetSearchResult { get; set; }

        void SetTargetNameWithoutSearch(string targetName);
    }
}