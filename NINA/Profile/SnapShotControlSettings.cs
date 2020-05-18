#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyFilterWheel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
            get {
                return gain;
            }
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
            get {
                return filter;
            }
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
            get {
                return exposureDuration;
            }
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
            get {
                return save;
            }
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
            get {
                return loop;
            }
            set {
                if (loop != value) {
                    loop = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
