using NINA.Utility.Mediator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    class ProfileManager {

        private ProfileManager() {
            Load();
        }

        private static readonly Lazy<ProfileManager> lazy =
        new Lazy<ProfileManager>(() => new ProfileManager());

        public static ProfileManager Instance { get { return lazy.Value; } }

        string PROFILEFILEPATH = Path.Combine(Utility.APPLICATIONTEMPPATH, "profiles.settings");


        public Profiles Profiles { get; set; }

        public void Add() {
            Profiles.Add(new Profile("Profile" + (Profiles.ProfileList.Count + 1)));
        }

        public void Save() {
            try {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Profiles));

                using (StreamWriter writer = new StreamWriter(PROFILEFILEPATH)) {
                    xmlSerializer.Serialize(writer, Profiles);
                }

            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.Notification.ShowError(ex.Message);
            }
        }

        public Profile ActiveProfile {
            get {
                return Profiles.ActiveProfile;
            }
        }

        private void Load() {
            if (File.Exists(PROFILEFILEPATH)) {
                try {
                    var profilesXml = XElement.Load(PROFILEFILEPATH);

                    System.IO.StringReader reader = new System.IO.StringReader(profilesXml.ToString());
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Profiles));

                    Profiles = (Profiles)xmlSerializer.Deserialize(reader);
                    Profiles.SelectActiveProfile();
                } catch (Exception ex) {
                    LoadDefaultProfile();
                    Logger.Error(ex);
                    System.Windows.MessageBox.Show("Profile file is corrupt. Loading default profile. \n" + ex.Message);
                }
            } else {
                LoadDefaultProfile();
            }
        }

        public void SelectProfile(Guid guid) {
            Save();
            Profiles.SelectProfile(guid);
        }

        private void LoadDefaultProfile() {
            Profiles = new Profiles();
            Profiles.Add(new Profile("Default"));
            SelectProfile(Profiles.ProfileList[0].Id);
        }

        public IEnumerable<Profile> GetProfiles() {
            return Profiles.ProfileList;
        }
    }
}
