using NINA.Utility;
using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NINA.Model.MyFilterWheel {

    internal interface IFilterWheel : IDevice {
        short InterfaceVersion { get; }
        int[] FocusOffsets { get; }
        string[] Names { get; }
        short Position { get; set; }
        ArrayList SupportedActions { get; }
        AsyncObservableCollection<FilterInfo> Filters { get; }
    }

    [Serializable()]
    [DataContract]
    public class FilterInfo : BaseINPC {

        private FilterInfo() {
        }

        private string _name;
        private int _focusOffset;
        private short _position;
        private double _autoFocusExposureTime;
        private FlatWizardFilterSettings _flatWizardFilterSettings;

        [DataMember(Name = nameof(_name))]
        public string Name {
            get {
                return _name;
            }

            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_focusOffset))]
        public int FocusOffset {
            get {
                return _focusOffset;
            }

            set {
                _focusOffset = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_position))]
        public short Position {
            get {
                return _position;
            }

            set {
                _position = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(_autoFocusExposureTime))]
        public double AutoFocusExposureTime {
            get {
                return _autoFocusExposureTime;
            }

            set {
                _autoFocusExposureTime = value;
                RaisePropertyChanged();
            }
        }

        [DataMember(Name = nameof(FlatWizardFilterSettings), IsRequired = false)]
        public FlatWizardFilterSettings FlatWizardFilterSettings {
            get {
                return _flatWizardFilterSettings;
            }
            set {
                _flatWizardFilterSettings = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo(string n, int offset, short position) {
            Name = n;
            FocusOffset = offset;
            Position = position;
            FlatWizardFilterSettings = new FlatWizardFilterSettings();
        }

        public FilterInfo(string n, int offset, short position, double autoFocusExposureTime) : this(n, offset, position) {
            AutoFocusExposureTime = autoFocusExposureTime;
            FlatWizardFilterSettings = new FlatWizardFilterSettings();
        }

        public override string ToString() {
            return Name;
        }
    }

    [Serializable]
    [DataContract]
    public class FlatWizardFilterSettings : BaseINPC {

        public FlatWizardFilterSettings() {
            HistogramMeanTarget = 0.5;
            HistogramTolerance = 0.1;
            StepSize = 0.1;
            MinFlatExposureTime = 0.01;
            MaxFlatExposureTime = 30;
            BinningMode = new MyCamera.BinningMode(1, 1);
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

        private MyCamera.BinningMode binningMode;

        [DataMember]
        public MyCamera.BinningMode BinningMode {
            get {
                return binningMode;
            }
            set {
                binningMode = value;
                RaisePropertyChanged();
            }
        }

        private double minFlatExposureTime;

        [DataMember]
        public double MinFlatExposureTime {
            get {
                return minFlatExposureTime;
            }
            set {
                minFlatExposureTime = value;
                RaisePropertyChanged();
            }
        }

        private double maxFlatExposureTime;

        [DataMember]
        public double MaxFlatExposureTime {
            get {
                return maxFlatExposureTime;
            }
            set {
                maxFlatExposureTime = value;
                RaisePropertyChanged();
            }
        }
    }
}