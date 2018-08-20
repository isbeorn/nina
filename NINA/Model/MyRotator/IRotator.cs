using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyRotator {

    internal interface IRotator : IDevice {
        bool IsMoving { get; }
        bool Connected { get; }

        float Position { get; }

        void Move(float position);

        void MoveAbsolute(float position);

        void Halt();
    }
}