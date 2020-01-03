#region "copyright"

/*
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

/*
 * Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class WeatherDataSettings : Settings, IWeatherDataSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            Id = "No_Device";
            DisplayFahrenheit = false;
            DisplayImperial = false;
            OpenWeatherMapAPIKey = string.Empty;
        }

        private string id = string.Empty;

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private bool displayFahrenheit = false;

        [DataMember]
        public bool DisplayFahrenheit {
            get {
                return displayFahrenheit;
            }
            set {
                displayFahrenheit = value;
                RaisePropertyChanged();
            }
        }

        private bool displayImperial = false;

        [DataMember]
        public bool DisplayImperial {
            get {
                return displayImperial;
            }
            set {
                displayImperial = value;
                RaisePropertyChanged();
            }
        }

        private string openWeatherMapAPIKey = string.Empty;

        [DataMember]
        public string OpenWeatherMapAPIKey {
            get {
                return openWeatherMapAPIKey;
            }
            set {
                openWeatherMapAPIKey = value;
                RaisePropertyChanged();
            }
        }
    }
}