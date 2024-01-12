#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Profile.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class FlatWizardSettings : Settings, IFlatWizardSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            flatCount = 10;
            histogramTolerance = 0.1;
            histogramMeanTarget = 0.5;
            darkFlatCount = 0;
            altitudeSite = AltitudeSite.EAST;
            flatWizardMode = FlatWizardMode.DYNAMICEXPOSURE;
        }

        private int flatCount;

        [DataMember]
        public int FlatCount {
            get => flatCount;
            set {
                if (flatCount != value) {
                    flatCount = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double histogramMeanTarget;

        [DataMember]
        public double HistogramMeanTarget {
            get => histogramMeanTarget;
            set {
                if (histogramMeanTarget != value) {
                    histogramMeanTarget = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double histogramTolerance;

        [DataMember]
        public double HistogramTolerance {
            get => histogramTolerance;
            set {
                if (histogramTolerance != value) {
                    histogramTolerance = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int darkFlatCount;

        [DataMember]
        public int DarkFlatCount {
            get => darkFlatCount;
            set {
                if (darkFlatCount != value) {
                    darkFlatCount = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool openForDarkFlats;

        [DataMember]
        public bool OpenForDarkFlats {
            get => openForDarkFlats;
            set {
                if (openForDarkFlats == value) return;
                openForDarkFlats = value;
                RaisePropertyChanged();
            }
        }

        private AltitudeSite altitudeSite;

        [DataMember]
        public AltitudeSite AltitudeSite {
            get => altitudeSite;
            set {
                if (altitudeSite != value) {
                    altitudeSite = value;
                    RaisePropertyChanged();
                }
            }
        }

        private FlatWizardMode flatWizardMode;

        [DataMember]
        public FlatWizardMode FlatWizardMode {
            get => flatWizardMode;
            set {
                if (flatWizardMode != value) {
                    flatWizardMode = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}