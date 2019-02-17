using System;
using System.Collections.Generic;

namespace NINA.Model {

    internal class Constellation {

        public Constellation(string id) {
            Id = id;
            Name = Locale.Loc.Instance["LblConstellation_" + id];
            StarConnections = new List<Tuple<Star, Star>>();
        }

        public string Id { get; }

        public bool GoesOverRaZero { get; set; }

        public string Name { get; private set; }

        public List<Star> Stars { get; set; }

        public List<Tuple<Star, Star>> StarConnections { get; private set; }
    }
}