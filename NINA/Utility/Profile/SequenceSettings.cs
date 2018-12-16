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
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class SequenceSettings : Settings, ISequenceSettings {
        private string templatePath = string.Empty;

        [DataMember]
        public string TemplatePath {
            get {
                return templatePath;
            }
            set {
                templatePath = value;
                RaisePropertyChanged();
            }
        }

        private TimeSpan estimatedDownloadTime = TimeSpan.FromSeconds(0);

        public TimeSpan EstimatedDownloadTime {
            get {
                return estimatedDownloadTime;
            }
            set {
                estimatedDownloadTime = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public long TimeSpanInTicks {
            get {
                return estimatedDownloadTime.Ticks;
            }
            set {
                estimatedDownloadTime = new TimeSpan(value);
            }
        }
    }
}