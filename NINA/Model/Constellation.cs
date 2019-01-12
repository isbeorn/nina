using NINA.Utility;
using System;
using System.Collections.ObjectModel;

namespace NINA.Model {

    internal class Constellation : BaseINPC {
        private string name;
        private ObservableCollection<Tuple<Star, Star>> starConnections;
        private ObservableCollection<Star> stars;

        public Constellation(string id) {
            Id = id;
            Name = Locale.Loc.Instance["LblConstellation_" + id];
            StarConnections = new ObservableCollection<Tuple<Star, Star>>();
        }

        public string Id { get; }

        public bool GoesOverRaZero { get; set; }

        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<Star> Stars {
            get => stars;
            set {
                stars = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<Tuple<Star, Star>> StarConnections {
            get => starConnections;
            set {
                starConnections = value;
                RaisePropertyChanged();
            }
        }
    }
}