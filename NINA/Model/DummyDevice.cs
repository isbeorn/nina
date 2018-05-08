using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model {

    public class DummyDevice : IDevice {

        public DummyDevice(string name) {
            Name = name;
        }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        public string Id {
            get {
                return "No_Device";
            }
        }

        private string _name;

        public string Name {
            get {
                return _name;
            }
            private set {
                _name = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => false);
        }

        public void Disconnect() {
            return;
        }

        public void SetupDialog() {
        }
    }
}