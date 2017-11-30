using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model
{
    public class ApplicationStatus : BaseINPC {
        private string _source;
        public string Source {
            get {
                return _source;
            }
            set {
                _source = value;
                RaisePropertyChanged();
            }
        }

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();
            }
        }

        private double _statusValue;
        public double StatusValue {
            get {
                return _statusValue;
            }
            set {
                _statusValue = value;
                RaisePropertyChanged();
            }
        }

        private int _maxStatusValue;
        public int MaxStatusValue {
            get {
                return _maxStatusValue;
            }
            set {
                _maxStatusValue = value;
                RaisePropertyChanged();
            }
        }


    }
}
