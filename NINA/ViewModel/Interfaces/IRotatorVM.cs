using NINA.Model.MyRotator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.Interfaces {

    internal interface IRotatorVM : IDeviceVM<RotatorInfo> {

        Task<float> Move(float position);

        Task<float> MoveRelative(float position);
    }
}