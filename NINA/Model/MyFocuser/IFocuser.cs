using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser {
    interface IFocuser : IDevice {
        bool Connected { get; }
        bool Connect();
        string Description { get; }
        void Disconnect();
        bool IsMoving { get; }
        int MaxIncrement { get; }
        int MaxStep { get; }
        int Position { get; }
        double StepSize { get; }
        bool TempCompAvailable { get; }
        bool TempComp { get; set; }
        double Temperature { get; }
        void Move(int position);
        void Halt();
        void UpdateValues();
}
}
