#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ComponentModel;
using NINA.Model.MyFilterWheel;
using System.IO;
using NINA.Utility;

namespace NINA.Profile {

    /// <summary>
    /// These classes are obsolete and purely there for migration purposes of profiles from version 1.8.1 and below
    /// </summary>
    [Serializable()]
    [DataContract]
    [KnownType(typeof(NINA.Profile.Profile))]
    [Obsolete]
    public class Profiles : BaseINPC {

        public Profiles() {
            ProfileList = new ObserveAllCollection<IProfile>();
        }

        [DataMember(Name = nameof(Profile))]
        public ObserveAllCollection<IProfile> ProfileList { get; set; }

        private Guid activeProfileId;

        [DataMember]
        public Guid ActiveProfileId {
            get {
                return activeProfileId;
            }
            set {
                activeProfileId = value;
            }
        }

        public IProfile ActiveProfile { get; private set; }

        public void Add(IProfile p) {
            ProfileList.Add(p);
        }

        public void SelectProfile(Guid id) {
            var p = this.ProfileList.FirstOrDefault((x) => x.Id == id);
            this.ActiveProfile = p;
            this.ActiveProfileId = p.Id;
        }
    }
}
