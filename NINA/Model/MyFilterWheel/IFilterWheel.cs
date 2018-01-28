using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Model.MyFilterWheel {
    interface IFilterWheel : IDevice {
        bool Connected { get; }
        string Description { get; }
        string DriverInfo { get; }
        string DriverVersion { get; }
        short InterfaceVersion { get; }
        int[] FocusOffsets { get; }
        string[] Names { get; }
        short Position { get; set; }
        ArrayList SupportedActions { get; }
        AsyncObservableCollection<FilterInfo> Filters { get; }


    }

    [Serializable()]
    [XmlRoot(ElementName = nameof(FilterInfo))]
    public class FilterInfo : BaseINPC {
        private FilterInfo() { }
        private string _name;
        private int _focusOffset;
        private short _position;
        private double _autoFocusExposureTime;

        [XmlElement(nameof(Name))]
        public string Name {
            get {
                return _name;
            }

            set {
                _name = value;
                RaisePropertyChanged();
            }
        }
        [XmlElement(nameof(FocusOffset))]
        public int FocusOffset {
            get {
                return _focusOffset;
            }

            set {
                _focusOffset = value;
                RaisePropertyChanged();
            }
        }
        [XmlElement(nameof(Position))]
        public short Position {
            get {
                return _position;
            }

            set {
                _position = value;
                RaisePropertyChanged();
            }
        }

        [XmlElement(nameof(AutoFocusExposureTime))]
        public double AutoFocusExposureTime {
            get {
                return _autoFocusExposureTime;
            }

            set {
                _autoFocusExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo(string n, int offset, short position) {
            Name = n;
            FocusOffset = offset;
            Position = position;
        }

        public FilterInfo(string n, int offset, short position, double autoFocusExposureTime) : this(n, offset, position) {
            AutoFocusExposureTime = autoFocusExposureTime;
        }

        public override string ToString() {
            return Name;
        }
    }
}
