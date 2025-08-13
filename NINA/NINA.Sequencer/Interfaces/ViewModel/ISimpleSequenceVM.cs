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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility.WindowService;
using NINA.Sequencer.Container;

namespace NINA.ViewModel.Interfaces {

    public interface ISimpleSequenceVM {
        ICommand AddTargetCommand { get; }
        TimeSpan EstimatedDownloadTime { get; set; }
        ObservableCollection<string> ImageTypes { get; set; }
        TimeSpan OverallDuration { get; }
        DateTime OverallEndTime { get; }
        DateTime OverallStartTime { get; }
        ICommand SaveAsSequenceCommand { get; }
        ICommand SaveSequenceCommand { get; }
        ICommand SaveTargetSetCommand { get; }
        ICommand BuildSequenceCommand { get; }
        ISimpleDSOContainer SelectedTarget { get; set; }
        IWindowServiceFactory WindowServiceFactory { get; set; }
        NINA.Sequencer.ISequencer Sequencer { get; }

        Task Initialize();

        bool LoadTarget();

        bool LoadTargetSet();

        bool ImportTargets();

        void AddDownloadTime(TimeSpan t);

        void AddTarget(IDeepSkyObject deepSkyObject);
        event Func<object, EventArgs, Task> SequenceStarting;
        event Func<object, EventArgs, Task> SequenceFinished;
    }
}