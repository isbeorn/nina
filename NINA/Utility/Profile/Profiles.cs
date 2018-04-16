using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(Profiles))]
    public class Profiles : BaseINPC {
        public Profiles() {
            ProfileList = new ObserveAllCollection<Profile>();
        }

        [XmlElement(nameof(Profile))]
        public ObserveAllCollection<Profile> ProfileList { get; set; }

        private Guid activeProfileId;
        [XmlAttribute(nameof(ActiveProfileId))]
        public Guid ActiveProfileId {
            get {
                return activeProfileId;
            }
            set {
                activeProfileId = value;
            }
        }

        private Profile activeProfile;
        [XmlIgnore]
        public Profile ActiveProfile {
            get {
                return activeProfile;
            }
            private set {
                activeProfile = value;
            }
        }

        public void Add(Profile p) {
            ProfileList.Add(p);
        }

        public void SelectActiveProfile() {
            SelectProfile(ActiveProfileId);
        }

        public void SelectProfile(Guid id) {
            if (this.ActiveProfile != null) this.ActiveProfile.IsActive = false;

            var p = this.ProfileList.FirstOrDefault((x) => x.Id == id);
            this.ActiveProfile = p;
            this.ActiveProfile.IsActive = true;
            this.ActiveProfileId = p.Id;
        }
    }
}
