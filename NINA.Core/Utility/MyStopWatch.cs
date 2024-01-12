#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NINA.Core.Utility {

    public sealed class MyStopWatch : IDisposable {
        private Stopwatch _stopWatch;
        private string _memberName;
        private string _filePath;
        private DateTime _startTime;
        private DateTime _stopTime;

        private MyStopWatch(string memberName, string filePath) {
            this._memberName = memberName;
            this._filePath = filePath;
            this._startTime = DateTime.UtcNow;
            _stopWatch = Stopwatch.StartNew();
        }

        public TimeSpan Elapsed => _stopWatch.Elapsed;

        private void Log() {
            string message = string.Format("Start: {0}; Stopped: {1}; Elapsed: {2}", _startTime.ToString("dd.MM.yyyy hh:mm:ss.fff"), _stopTime.ToString("dd.MM.yyyy hh:mm:ss.fff"), _stopWatch.Elapsed);
            Logger.Trace(message, _memberName, _filePath);
        }

        void IDisposable.Dispose() {
            this._stopWatch.Stop();
            this._stopTime = DateTime.UtcNow;
            Log();
        }

        public static MyStopWatch Measure(
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string filePath = "") {
            return new MyStopWatch(memberName, filePath);
        }
    }
}