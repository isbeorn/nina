#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.ObjectModel;
using System.Windows.Input;
using NINA.Equipment.Equipment.MyCamera;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Model.Equipment;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IAnchorablePlateSolverVM : IDockableVM {
        IProfile ActiveProfile { get; }
        CameraInfo CameraInfo { get; }
        ICommand CancelSolveCommand { get; }
        PlateSolveResult PlateSolveResult { get; set; }
        ObservableCollection<PlateSolveResult> PlateSolveResultList { get; set; }
        double RepeatThreshold { get; set; }
        bool SlewToTarget { get; set; }
        BinningMode SnapBin { get; set; }
        double SnapExposureDuration { get; set; }
        FilterInfo SnapFilter { get; set; }
        int SnapGain { get; set; }
        IAsyncCommand SolveCommand { get; }
        ApplicationStatus Status { get; set; }
        bool Sync { get; set; }
        TelescopeInfo TelescopeInfo { get; }

        void Dispose();

        void UpdateDeviceInfo(CameraInfo cameraInfo);

        void UpdateDeviceInfo(TelescopeInfo telescopeInfo);
    }
}