#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows.Threading {
    /// <summary>
    /// Stub implementation of WPF Dispatcher for headless execution
    /// </summary>
    public class Dispatcher {
        // Overload for WPF compatibility
        public void Invoke(DispatcherPriority priority, Action callback) {
            callback();
        }

        // Overload for WPF compatibility
        public DispatcherOperation BeginInvoke(DispatcherPriority priority, Action callback) {
            var op = new DispatcherOperation {
                Dispatcher = this,
                Priority = priority,
                Status = DispatcherOperationStatus.Completed,
                Task = System.Threading.Tasks.Task.Run(callback)
            };
            callback();
            return op;
        }
        private static Dispatcher _currentDispatcher = new Dispatcher();

        public static Dispatcher CurrentDispatcher => _currentDispatcher;

        public T Invoke<T>(Func<T> callback) {
            // In headless mode, just execute the callback directly
            return callback();
        }

        public void Invoke(Action callback) {
            callback();
        }

        public object Invoke(Delegate method, params object[] args) {
            // Execute the delegate with the provided arguments
            return method?.DynamicInvoke(args);
        }

        public System.Threading.Tasks.Task InvokeAsync(Action callback) {
            callback();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task<T> InvokeAsync<T>(Func<T> callback) {
            return System.Threading.Tasks.Task.FromResult(callback());
        }

        public System.Threading.Tasks.Task BeginInvoke(Action callback) {
            return System.Threading.Tasks.Task.Run(callback);
        }


        public System.Threading.Tasks.Task BeginInvoke(DispatcherPriority priority, Delegate method, params object[] args) {
            // Ignore priority in headless mode
            return System.Threading.Tasks.Task.Run(() => method?.DynamicInvoke(args));
        }
    }

    /// <summary>
    /// Priority levels for dispatcher operations (stub - priorities ignored in headless mode)
    /// </summary>
    public enum DispatcherPriority {
        Invalid = -1,
        Inactive = 0,
        SystemIdle = 1,
        ApplicationIdle = 2,
        ContextIdle = 3,
        Background = 4,
        Input = 5,
        Loaded = 6,
        Render = 7,
        DataBind = 8,
        Normal = 9,
        Send = 10
    }

    /// <summary>
    /// Timer that executes on the dispatcher (stub - uses System.Timers.Timer in headless mode)
    /// </summary>
    public class DispatcherTimer {
        private System.Timers.Timer _timer;

        public DispatcherTimer() {
            _timer = new System.Timers.Timer();
            _timer.Elapsed += (s, e) => Tick?.Invoke(this, EventArgs.Empty);
        }

        public DispatcherTimer(DispatcherPriority priority) {
            // Ignore priority in headless mode
            _timer = new System.Timers.Timer();
            _timer.Elapsed += (s, e) => Tick?.Invoke(this, EventArgs.Empty);
        }

        public DispatcherTimer(DispatcherPriority priority, Dispatcher dispatcher) {
            // Ignore priority and dispatcher in headless mode
            _timer = new System.Timers.Timer();
            _timer.Elapsed += (s, e) => Tick?.Invoke(this, EventArgs.Empty);
        }

        public TimeSpan Interval {
            get => TimeSpan.FromMilliseconds(_timer.Interval);
            set => _timer.Interval = value.TotalMilliseconds;
        }

        public bool IsEnabled {
            get => _timer.Enabled;
            set => _timer.Enabled = value;
        }

        public event EventHandler Tick;

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();
    }
}
