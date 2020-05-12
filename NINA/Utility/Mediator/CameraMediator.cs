#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Model.MyCamera;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.Camera;
using System;
using System.Collections.Async;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class CameraMediator : DeviceMediator<ICameraVM, ICameraConsumer, CameraInfo>, ICameraMediator {

        public Task Capture(CaptureSequence sequence, CancellationToken token,
            IProgress<ApplicationStatus> progress) {
            return handler.Capture(sequence, token, progress);
        }

        public IAsyncEnumerable<IExposureData> LiveView(CancellationToken token) {
            return handler.LiveView(token);
        }

        public Task<IExposureData> Download(CancellationToken token) {
            return handler.Download(token);
        }

        public Task<bool> CoolCamera(double temperature, TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return handler.CoolCamera(temperature, duration, progress, ct);
        }

        public Task<bool> WarmCamera(TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return handler.WarmCamera(duration, progress, ct);
        }

        public void AbortExposure() {
            handler.AbortExposure();
        }

        public void SetBinning(short x, short y) {
            handler.SetBinning(x, y);
        }

        public void SetGain(short gain) {
            handler.SetGain(gain);
        }

        public void SetSubSample(bool subSample) {
            handler.SetSubSample(subSample);
        }

        public void SetSubSampleArea(int x, int y, int width, int height) {
            handler.SetSubSampleArea(x, y, width, height);
        }

        public bool AtTargetTemp {
            get {
                return handler.AtTargetTemp;
            }
        }

        public double TargetTemp {
            get {
                return handler.TargetTemp;
            }
        }
    }
}