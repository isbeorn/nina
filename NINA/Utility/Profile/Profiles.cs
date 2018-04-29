using NINA.Utility.Mediator;
using System;
using System.Linq;
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

            Mediator.Mediator.Instance.Notify(MediatorMessages.LocationChanged, null);
            Mediator.Mediator.Instance.Notify(MediatorMessages.ProfileChanged, null);

            System.Threading.Thread.CurrentThread.CurrentUICulture = ActiveProfile.ApplicationSettings.Language;
            System.Threading.Thread.CurrentThread.CurrentCulture = ActiveProfile.ApplicationSettings.Language;
            Locale.Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);
        }
    }
}