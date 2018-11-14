using NINA.Utility;
using System;
using System.Runtime.Serialization;

namespace NINA.Model.MyFilterWheel {

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
}