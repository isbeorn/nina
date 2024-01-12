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
using NINA.Profile.Interfaces;
using System.Runtime.Serialization;

namespace NINA.Profile {

    public class SnapShotControlSettings : Settings, ISnapShotControlSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            gain = -1;
            exposureDuration = 1;
            filter = null;
            loop = false;
            save = false;
        }

        private int gain;

        [DataMember]
        public int Gain {
            get => gain;
            set {
                if (gain != value) {
                    gain = value;
                    RaisePropertyChanged();
                }
            }
        }

        private FilterInfo filter;

        [DataMember]
        public FilterInfo Filter {
            get => filter;
            set {
                if (filter != value) {
                    filter = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double exposureDuration;

        [DataMember]
        public double ExposureDuration {
            get => exposureDuration;
            set {
                if (exposureDuration != value) {
                    exposureDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool save;

        [DataMember]
        public bool Save {
            get => save;
            set {
                if (save != value) {
                    save = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool loop;

        [DataMember]
        public bool Loop {
            get => loop;
            set {
                if (loop != value) {
                    loop = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}