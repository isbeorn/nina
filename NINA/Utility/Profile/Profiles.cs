#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility.Mediator;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    [KnownType(typeof(Profile))]
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

        public void SelectActiveProfile() {
            var id = ActiveProfileId;
            if (id == Guid.Empty) {
                var p = this.ProfileList.Where((x) => x.IsActive = true).FirstOrDefault();
                if (p == null) {
                    id = this.ProfileList[0].Id;
                } else {
                    id = p.Id;
                }
            }
            SelectProfile(id);
        }

        public void SelectProfile(Guid id) {
            if (this.ActiveProfile != null) this.ActiveProfile.IsActive = false;

            var p = this.ProfileList.FirstOrDefault((x) => x.Id == id);
            this.ActiveProfile = p;
            this.ActiveProfile.IsActive = true;
            this.ActiveProfileId = p.Id;

            System.Threading.Thread.CurrentThread.CurrentUICulture = ActiveProfile.ApplicationSettings.Language;
            System.Threading.Thread.CurrentThread.CurrentCulture = ActiveProfile.ApplicationSettings.Language;
            Locale.Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);
        }
    }
}