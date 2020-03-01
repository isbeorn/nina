#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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