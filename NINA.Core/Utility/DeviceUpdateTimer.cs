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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Core.Utility {

    public interface IDeviceUpdateTimerFactory {

        IDeviceUpdateTimer Create(Func<Dictionary<string, object>> getValuesFunc, Action<Dictionary<string, object>> updateValuesFunc, double interval);
    }

    public class DefaultDeviceUpateTimerFactory : IDeviceUpdateTimerFactory {

        public DefaultDeviceUpateTimerFactory() {
        }

        public IDeviceUpdateTimer Create(Func<Dictionary<string, object>> getValuesFunc, Action<Dictionary<string, object>> updateValuesFunc, double interval) {
            return new DeviceUpdateTimer(getValuesFunc, updateValuesFunc, interval);
        }
    }

    public interface IDeviceUpdateTimer {
        Func<Dictionary<string, object>> GetValuesFunc { get; }
        IProgress<Dictionary<string, object>> Progress { get; }
        double Interval { get; set; }

        Task Stop();
        Task Run();
        Task WaitForNextUpdate(CancellationToken ct);
        [Obsolete("Superseded by \"Run\"")]
        void Start();
    }

    public class DeviceUpdateTimer : IDeviceUpdateTimer {

        public DeviceUpdateTimer(Func<Dictionary<string, object>> getValuesFunc, Action<Dictionary<string, object>> updateValuesFunc, double interval) {
            GetValuesFunc = getValuesFunc;
            Interval = interval;
            Progress = new Progress<Dictionary<string, object>>(updateValuesFunc);
        }

        private CancellationTokenSource cts;
        private Task task;
        public Func<Dictionary<string, object>> GetValuesFunc { get; private set; }
        public IProgress<Dictionary<string, object>> Progress { get; private set; }
        public DateTimeOffset LastUpdate { get; private set; } = DateTimeOffset.MinValue;
        public double Interval { get; set; }

        public async Task Stop() {
            try { cts?.Cancel(); } catch { }
            while (!task?.IsCompleted == true) {
                await Task.Delay(100);
            }
        }

        public async Task WaitForNextUpdate(CancellationToken ct) {
            var now = DateTimeOffset.UtcNow;
            var destination = now + TimeSpan.FromSeconds(Interval);
            while (!ct.IsCancellationRequested && !task?.IsCompleted == true && LastUpdate < destination) {
                await Task.Delay(50, ct);
            }
        }

        [Obsolete("Superseded by \"Run\"")]
        public void Start() {
            _ = Run();
        }

        public async Task Run() {
            task = Task.Run(async () => {
                try { cts?.Dispose(); } catch { }
                cts = new CancellationTokenSource();
                var token = cts.Token;

                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Interval));

                Dictionary<string, object> values = new Dictionary<string, object>();
                try {
                    while (await timer.WaitForNextTickAsync(token) && !token.IsCancellationRequested) {
                        values = GetValuesFunc();

                        Progress.Report(values);

                        LastUpdate = DateTimeOffset.UtcNow;
                    }
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                } finally {
                    values.Clear();
                    values.Add("Connected", false);
                    Progress.Report(values);
                }
            });
            await task;
        }
    }
}