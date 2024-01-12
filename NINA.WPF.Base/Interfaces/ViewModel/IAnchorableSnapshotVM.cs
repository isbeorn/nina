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
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Core.Utility;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Equipment.MyFilterWheel;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IAnchorableSnapshotVM : IDockableVM {
        CameraInfo CameraInfo { get; set; }
        FilterWheelInfo FilterWheelInfo { get; set; }
        ICommand CancelSnapCommand { get; }
        bool IsLooping { get; set; }
        bool LiveViewEnabled { get; set; }
        bool Loop { get; set; }
        BinningMode SnapBin { get; set; }
        IAsyncCommand SnapCommand { get; }
        double SnapExposureDuration { get; set; }
        FilterInfo SnapFilter { get; set; }
        int SnapGain { get; set; }
        bool SnapSave { get; set; }
        bool SnapSubSample { get; set; }
        IAsyncCommand StartLiveViewCommand { get; }
        ApplicationStatus Status { get; set; }
        ICommand StopLiveViewCommand { get; }

        void Dispose();

        Task<bool> SnapImage(IProgress<ApplicationStatus> progress);

        void UpdateDeviceInfo(CameraInfo cameraStatus);
        void UpdateDeviceInfo(FilterWheelInfo FilterWheelInfo);
    }
}