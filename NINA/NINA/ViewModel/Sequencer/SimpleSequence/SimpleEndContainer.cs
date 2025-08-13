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

    public class SimpleEndContainer : ParallelContainer {
        private WarmCamera warmInstruction;
        private ParkScope parkInstruction;
        private DewHeater dewHeaterInstruction;
        private IProfileService profileService;
        private ICameraMediator cameraMediator;

        public SimpleEndContainer(ISequencerFactory factory, IProfileService profileService, ICameraMediator cameraMediator) {
            this.cameraMediator = cameraMediator;
            this.dewHeaterInstruction = factory.GetItem<DewHeater>();
            this.dewHeaterInstruction.OnOff = false;
            this.warmInstruction = factory.GetItem<WarmCamera>();
            warmInstruction.Duration = profileService.ActiveProfile.CameraSettings.WarmingDuration;
            this.parkInstruction = factory.GetItem<ParkScope>();
            this.profileService = profileService;
            WarmCamAtSequenceEnd = profileService.ActiveProfile.SequenceSettings.WarmCamAtSequenceEnd;
            ParkMountAtSequenceEnd = profileService.ActiveProfile.SequenceSettings.ParkMountAtSequenceEnd;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if(WarmCamAtSequenceEnd && cameraMediator.GetInfo().HasDewHeater) {
                this.dewHeaterInstruction.ResetProgress();
                await this.dewHeaterInstruction.Run(progress, token);
            }
            warmInstruction.Duration = profileService.ActiveProfile.CameraSettings.WarmingDuration;
            await base.Execute(progress, token);
        }

        [JsonProperty]
        public bool WarmCamAtSequenceEnd {
            get => profileService.ActiveProfile.SequenceSettings.WarmCamAtSequenceEnd;
            set {
                profileService.ActiveProfile.SequenceSettings.WarmCamAtSequenceEnd = value;
                if (value) {
                    this.Add(warmInstruction);
                } else {
                    this.Remove(warmInstruction);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool ParkMountAtSequenceEnd {
            get => profileService.ActiveProfile.SequenceSettings.ParkMountAtSequenceEnd;
            set {
                profileService.ActiveProfile.SequenceSettings.ParkMountAtSequenceEnd = value;
                if (value) {
                    this.Add(parkInstruction);
                } else {
                    this.Remove(parkInstruction);
                }
                RaisePropertyChanged();
            }
        }
    }
}