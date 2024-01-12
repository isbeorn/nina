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
using NINA.Equipment.Interfaces.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IDockManagerVM {
        List<IDockableVM> AnchorableInfoPanels { get; }
        List<IDockableVM> Anchorables { get; }
        List<IDockableVM> AnchorableTools { get; }
        IAsyncCommand LoadAvalonDockLayoutCommand { get; }
        ICommand ResetDockLayoutCommand { get; }

        Task<bool> InitializeAvalonDockLayout(object o);

        void SaveAvalonDockLayout();
    }
}