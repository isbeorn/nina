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

namespace NINA.Profile.Interfaces {

    public interface ISequenceSettings : ISettings {

        /// <summary>
        /// Used by Sequence Constructor
        /// </summary>
        TimeSpan EstimatedDownloadTime { get; set; }

        /// <summary>
        /// Used by Sequence Constructor
        /// </summary>
        string TemplatePath { get; set; }

        /// <summary>
        /// Used by Sequence Constructor
        /// </summary>
        long TimeSpanInTicks { get; set; }

        /// <summary>
        /// Used by Sequence Constructor
        /// </summary>
        bool ParkMountAtSequenceEnd { get; set; }

        /// <summary>
        /// Used by Sequence Constructor
        /// </summary>
        bool CloseDomeShutterAtSequenceEnd { get; set; }

        /// <summary>
        /// Used by Sequence Constructor
        /// </summary>
        bool ParkDomeAtSequenceEnd { get; set; }

        /// <summary>
        /// Used by Sequence Constructor
        /// </summary>
        bool WarmCamAtSequenceEnd { get; set; }

        /// <summary>
        /// Used by Sequence Constructor
        /// </summary>
        string SequenceCompleteCommand { get; set; }

        string DefaultSequenceFolder { get; set; }

        string StartupSequenceTemplate { get; set; }
        string SequencerTemplatesFolder { get; set; }
        string SequencerTargetsFolder { get; set; }
        bool CollapseSequencerTemplatesByDefault { get; set; }

        /// <summary>
        /// Used by Sequence Constructor
        /// </summary>
        bool CoolCameraAtSequenceStart { get; set; }

        /// <summary>
        /// Used by Sequence Builder
        /// </summary>
        bool UnparMountAtSequenceStart { get; set; }

        /// <summary>
        /// Used by Sequence Builder
        /// </summary>
        bool OpenDomeShutterAtSequenceStart { get; set; }

        /// <summary>
        /// Used by Sequence Builder
        /// </summary>
        bool DoMeridianFlip { get; set; }

        /// <summary>
        /// Disables the sequence dashboard and directly shows the advanced sequencer
        /// </summary>
        bool DisableSimpleSequencer { get; set; }
    }
}