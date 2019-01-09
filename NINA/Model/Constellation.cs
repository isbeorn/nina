using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NINA.Model {

    internal class Constellation : BaseINPC {
        private string name;
        private ObservableCollection<Tuple<Star, Star>> starConnections;

        public Constellation(string id) {
            Id = id;
            Name = Locale.Loc.Instance["LblConstellation_" + id];
            StarConnections = new ObservableCollection<Tuple<Star, Star>>();
        }

        public string Id { get; }

        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        public List<Star> Stars => StarConnections.Select(t => t.Item1).Concat(StarConnections.Select(t => t.Item2)).GroupBy(b => b.Name).Select(b => b.First()).ToList();

        public ObservableCollection<Tuple<Star, Star>> StarConnections {
            get => starConnections;
            set {
                starConnections = value;
                RaisePropertyChanged();
            }
        }
    }
}