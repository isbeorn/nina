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