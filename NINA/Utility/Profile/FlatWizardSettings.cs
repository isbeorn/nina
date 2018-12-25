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

using NINA.Model.MyCamera;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class FlatWizardSettings : Settings, IFlatWizardSettings {

        public FlatWizardSettings() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDeserialization(StreamingContext context) {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
            flatCount = 10;
            histogramTolerance = 0.1;
            histogramMeanTarget = 0.5;
            stepSize = 0.5;
            binningMode = new BinningMode(1, 1);
            darkFlatCount = 30;
        }

        private int flatCount;

        [DataMember]
        public int FlatCount {
            get {
                return flatCount;
            }
            set {
                flatCount = value;
                RaisePropertyChanged();
            }
        }

        private double histogramMeanTarget;

        [DataMember]
        public double HistogramMeanTarget {
            get {
                return histogramMeanTarget;
            }
            set {
                histogramMeanTarget = value;
                RaisePropertyChanged();
            }
        }

        private double histogramTolerance;

        [DataMember]
        public double HistogramTolerance {
            get {
                return histogramTolerance;
            }
            set {
                histogramTolerance = value;
                RaisePropertyChanged();
            }
        }

        private double stepSize;

        [DataMember]
        public double StepSize {
            get {
                return stepSize;
            }
            set {
                stepSize = value;
                RaisePropertyChanged();
            }
        }

        private BinningMode binningMode;

        [DataMember]
        public BinningMode BinningMode {
            get {
                return binningMode;
            }
            set {
                binningMode = value;
                RaisePropertyChanged();
            }
        }

        private int darkFlatCount;

        [DataMember]
        public int DarkFlatCount {
            get => darkFlatCount;
            set {
                darkFlatCount = value;
                RaisePropertyChanged();
            }
        }
    }
}