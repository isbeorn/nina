using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model {

    internal class DeviceInfo : BaseINPC {
        private bool connected;
        public bool Connected { get { return connected; } set { connected = value; RaisePropertyChanged(); } }
        private string name;
        public string Name { get { return name; } set { name = value; RaisePropertyChanged(); } }

        public static T CreateDefaultInstance<T>() where T : DeviceInfo, new() {
            return new T() {
                Connected = false
            };
        }
    }
}