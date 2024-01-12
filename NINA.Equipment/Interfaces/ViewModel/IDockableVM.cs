#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.Equipment.Interfaces.ViewModel {

    public interface IDockableVM : INotifyPropertyChanged {
        bool CanClose { get; set; }
        string ContentId { get; }
        ICommand HideCommand { get; }
        ICommand ToggleSettingsCommand { get; }
        GeometryGroup ImageGeometry { get; set; }
        bool IsClosed { get; set; }
        bool HasSettings { get; set; }
        bool IsVisible { get; set; }
        bool IsTool { get; }
        string Title { get; set; }

        void Hide(object o);
        void ToggleSettings(object o);
    }
}