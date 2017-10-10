using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model {
    interface IDevice : INotifyPropertyChanged {
        bool HasSetupDialog { get; }
        string Id { get; }
        void SetupDialog();             
    }
}
