#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Sequencer.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Conditions {

    public class ConditionWatchdog : IConditionWatchdog {
        private CancellationTokenSource watchdogCTS;
        private Task watchdogTask;
        private object lockObj = new object();

        public ConditionWatchdog(Func<Task> operation, TimeSpan delay) {
            WatchDogOperation = operation;
            Delay = delay;
        }

        public Func<Task> WatchDogOperation { get; }
        public TimeSpan Delay { get; set; }

        public Task WatchdogTask {
            get {
                lock (lockObj) {
                    return watchdogTask;
                }
            }
        }

        public Task Start() {
            lock (lockObj) {
                if (watchdogTask == null) {
                    watchdogCTS = new CancellationTokenSource();
                    var token = watchdogCTS.Token;
                    watchdogTask = Task.Run(async () => {
                        while (true) {
                            try {
                                await WatchDogOperation();
                            } catch (OperationCanceledException) {
                                Logger.Debug("Condition watchdog was canceled");                            
                            } catch (Exception ex) {
                                Logger.Error(ex);
                            }
                            await Task.Delay(Delay, token);
                        }
                    });
                }
                return watchdogTask;
            }
        }

        public void Cancel() {
            lock (lockObj) {
                try {
                    watchdogCTS?.Cancel();
                } catch { }

                watchdogCTS = null;
                watchdogTask = null;
            }
        }
    }
}