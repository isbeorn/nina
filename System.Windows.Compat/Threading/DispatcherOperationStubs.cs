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
    // Minimal stub for DispatcherOperationStatus
    public enum DispatcherOperationStatus {
        Pending,
        Aborted,
        Completed,
        Executing
    }

    // Minimal stub for DispatcherOperation
    public class DispatcherOperation {
        public DispatcherOperationStatus Status { get; set; }
        public Dispatcher Dispatcher { get; set; } = Dispatcher.CurrentDispatcher;
        public DispatcherPriority Priority { get; set; } = DispatcherPriority.Normal;
        public System.Threading.Tasks.Task Task { get; set; } = System.Threading.Tasks.Task.CompletedTask;
        public object Result { get; set; }
        public event System.EventHandler Aborted;
        public event System.EventHandler Completed;
        public bool Abort() { return true; }
        public DispatcherOperationStatus Wait() { return DispatcherOperationStatus.Completed; }
        public DispatcherOperationStatus Wait(System.TimeSpan timeout) { return DispatcherOperationStatus.Completed; }
        public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter() => Task.GetAwaiter();
    }
}
