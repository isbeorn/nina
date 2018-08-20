using NINA.Model.MyRotator;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface IRotatorMediator : IDeviceMediator<IRotatorVM, IRotatorConsumer, RotatorInfo> {

        Task<float> Move(float position);

        Task<float> MoveRelative(float position);
    }
}