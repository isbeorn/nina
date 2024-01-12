#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Astrometry;
using NINA.ViewModel.Sequencer.SimpleSequence;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyCamera;

namespace NINA.Sequencer.Container {

    public interface ISimpleDSOContainer : ISequenceHasChanged {
        ISimpleExposure ActiveExposure { get; set; }
        ICommand AddSimpleExposureCommand { get; }
        bool AutoFocusAfterHFRChange { get; set; }
        double AutoFocusAfterHFRChangeAmount { get; set; }
        bool AutoFocusAfterSetExposures { get; set; }
        bool AutoFocusAfterSetTime { get; set; }
        bool AutoFocusAfterTemperatureChange { get; set; }
        double AutoFocusAfterTemperatureChangeAmount { get; set; }
        bool AutoFocusOnFilterChange { get; set; }
        bool AutoFocusOnStart { get; set; }
        int AutoFocusSetExposures { get; set; }
        double AutoFocusSetTime { get; set; }
        CameraInfo CameraInfo { get; }
        bool CenterTarget { get; set; }
        ICommand CoordsFromPlanetariumCommand { get; }
        ICommand CoordsToFramingCommand { get; }
        int Delay { get; set; }
        ICommand DemoteSimpleExposureCommand { get; }
        TimeSpan EstimatedDuration { get; set; }
        DateTime EstimatedEndTime { get; set; }
        DateTime EstimatedStartTime { get; set; }
        string FileName { get; set; }
        SequenceMode Mode { get; set; }
        NighttimeData NighttimeData { get; }
        ICommand PromoteSimpleExposureCommand { get; }
        ICommand RemoveSimpleExposureCommand { get; }
        ICommand ResetSimpleExposureCommand { get; }
        int RotateIterations { get; set; }
        bool RotateTarget { get; set; }
        ISimpleExposure SelectedSimpleExposure { get; set; }
        bool SlewToTarget { get; set; }
        bool StartGuiding { get; set; }
        InputTarget Target { get; set; }

        ISimpleExposure AddSimpleExposure();

        TimeSpan CalculateEstimatedRuntime();

        object Clone();

        Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token);

        void MoveDown();

        void MoveUp();

        IDeepSkyObjectContainer TransformToDSOContainer();

        bool Validate();
    }
}