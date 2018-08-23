using NINA.Model.MyRotator;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class RotatorMediator : DeviceMediator<IRotatorVM, IRotatorConsumer, RotatorInfo>, IRotatorMediator {

        public Task<float> Move(float position) {
            return handler.Move(position);
        }

        public Task<float> MoveRelative(float position) {
            return handler.MoveRelative(position);
        }
    }
}