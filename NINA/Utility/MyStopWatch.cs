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

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NINA.Utility {

    internal sealed class MyStopWatch : IDisposable {
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

        private void Log() {
            string message = string.Format("Start: {0}; Stopped: {1}; Elapsed: {2}", _startTime.ToString("dd.MM.yyyy hh:mm:ss.fff"), _stopTime.ToString("dd.MM.yyyy hh:mm:ss.fff"), _stopWatch.Elapsed);
            Debug.Print(string.Format("Method: {0}; File: {1} ", _memberName, _filePath) + message);
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