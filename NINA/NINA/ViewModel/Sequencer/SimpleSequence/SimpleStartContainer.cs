#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.Telescope;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Sequencer.SimpleSequence {

    public class SimpleStartContainer : SequentialContainer {
        private CoolCamera coolInstruction;
        private UnparkScope unparkInstruction;
        private DewHeater dewHeaterInstruction;
        private IProfileService profileService;
        private ICameraMediator cameraMediator;

        public SimpleStartContainer(ISequencerFactory factory, IProfileService profileService, ICameraMediator cameraMediator) {
            this.cameraMediator = cameraMediator;
            this.dewHeaterInstruction = factory.GetItem<DewHeater>();
            this.dewHeaterInstruction.OnOff = true;
            this.coolInstruction = factory.GetItem<CoolCamera>();
            coolInstruction.Temperature = profileService.ActiveProfile.CameraSettings.Temperature ?? 0;
            coolInstruction.Duration = profileService.ActiveProfile.CameraSettings.CoolingDuration;
            this.unparkInstruction = factory.GetItem<UnparkScope>();
            this.profileService = profileService;
            CoolCameraAtSequenceStart = profileService.ActiveProfile.SequenceSettings.CoolCameraAtSequenceStart;
            UnparkMountAtSequenceStart = profileService.ActiveProfile.SequenceSettings.UnparMountAtSequenceStart;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (CoolCameraAtSequenceStart && cameraMediator.GetInfo().HasDewHeater) {
                this.dewHeaterInstruction.ResetProgress();
                await this.dewHeaterInstruction.Run(progress, token);
            }
            coolInstruction.Temperature = profileService.ActiveProfile.CameraSettings.Temperature ?? 0;
            coolInstruction.Duration = profileService.ActiveProfile.CameraSettings.CoolingDuration;
            await base.Execute(progress, token);
        }

        [JsonProperty]
        public bool CoolCameraAtSequenceStart {
            get => profileService.ActiveProfile.SequenceSettings.CoolCameraAtSequenceStart;
            set {
                profileService.ActiveProfile.SequenceSettings.CoolCameraAtSequenceStart = value;
                if (value) {
                    this.Add(coolInstruction);
                } else {
                    this.Remove(coolInstruction);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool UnparkMountAtSequenceStart {
            get => profileService.ActiveProfile.SequenceSettings.UnparMountAtSequenceStart;
            set {
                profileService.ActiveProfile.SequenceSettings.UnparMountAtSequenceStart = value;
                if (value) {
                    this.Add(unparkInstruction);
                } else {
                    this.Remove(unparkInstruction);
                }
                RaisePropertyChanged();
            }
        }
    }
}