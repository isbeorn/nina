#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyGuider;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class GuiderMediator : DeviceMediator<IGuiderVM, IGuiderConsumer, GuiderInfo>, IGuiderMediator {
        public bool IsUsingSynchronizedGuider => handler.GuiderIsSynchronized;

        public Task<bool> Dither(CancellationToken token) {
            return handler.Dither(token);
        }

        public Guid StartRMSRecording() {
            return handler.StartRMSRecording();
        }

        public RMS StopRMSRecording(Guid handle) {
            return handler.StopRMSRecording(handle);
        }

        public Task<bool> StartGuiding(CancellationToken token) {
            return handler.StartGuiding(token);
        }

        public Task<bool> StopGuiding(CancellationToken token) {
            return handler.StopGuiding(token);
        }

        public Task<bool> ResumeGuiding(CancellationToken token) {
            return handler.ResumeGuiding(token);
        }

        public Task<bool> PauseGuiding(CancellationToken token) {
            return handler.PauseGuiding(token);
        }

        public Task<bool> AutoSelectGuideStar(CancellationToken token) {
            return handler.AutoSelectGuideStar(token);
        }
    }
}