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
using NINA.Core.Utility.WindowService;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IVersionCheckVM {
        ICommand CancelDownloadCommand { get; set; }
        string Changelog { get; set; }
        IAsyncCommand DownloadCommand { get; set; }
        bool Downloading { get; set; }
        int Progress { get; set; }
        IAsyncCommand ShowDownloadCommand { get; set; }
        bool UpdateAvailable { get; set; }
        string UpdateAvailableText { get; set; }
        ICommand UpdateCommand { get; set; }
        bool UpdateReady { get; set; }
        IWindowServiceFactory WindowServiceFactory { get; set; }

        Task<bool> CheckUpdate();
    }
}