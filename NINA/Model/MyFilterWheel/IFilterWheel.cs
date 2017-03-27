using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {
    interface IFilterWheel :IDevice {
        bool Connected { get; }
        string Description { get; }
        string Name { get;  }
        string DriverInfo { get;  }
        string DriverVersion { get; }
        short InterfaceVersion { get;  }
        int[] FocusOffsets { get;  }
        string[] Names { get; }
        short Position { get; set; }
        ArrayList SupportedActions { get; }
        AsyncObservableCollection<FilterInfo> Filters { get; }


        bool Connect();
        void Disconnect();

    }

    public class FilterInfo : BaseINPC {
        private string _name;
        private int _focusOffset;
        private short _position;

        public string Name {
            get {
                return _name;
            }

            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public int FocusOffset {
            get {
                return _focusOffset;
            }

            set {
                _focusOffset = value;
                RaisePropertyChanged();
            }
        }

        public short Position {
            get {
                return _position;
            }

            set {
                _position = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo(string n, int offset, short position) {
            Name = n;
            FocusOffset = offset;
            Position = position;
        }
    }
}
