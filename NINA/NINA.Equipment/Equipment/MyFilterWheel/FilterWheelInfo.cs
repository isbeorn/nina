#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model.Equipment;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyFilterWheel {

    public class FilterWheelInfo : DeviceInfo {
        private bool isMoving;

        public bool IsMoving {
            get => isMoving;
            set { isMoving = value; RaisePropertyChanged(); }
        }

        private FilterInfo _selectedFilter;

        public FilterInfo SelectedFilter {
            get => _selectedFilter;
            set {
                _selectedFilter = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> supportedActions;

        public IList<string> SupportedActions {
            get => supportedActions;
            set {
                supportedActions = value;
                RaisePropertyChanged();
            }
        }
    }
}