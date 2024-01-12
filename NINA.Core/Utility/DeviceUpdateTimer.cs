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

        void Start();
        Task WaitForNextUpdate(CancellationToken ct);
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
        public DateTime LastUpdate { get; private set; } = DateTime.MinValue;
        public double Interval { get; set; }

        public async Task Stop() {
            try { cts?.Cancel(); } catch { }
            while (!task?.IsCompleted == true) {
                await Task.Delay(100);
            }
        }

        public async Task WaitForNextUpdate(CancellationToken ct) {
            var now = DateTime.UtcNow;
            var destination = now + TimeSpan.FromSeconds(Interval);
            while(!ct.IsCancellationRequested && !task?.IsCompleted == true && LastUpdate < destination) {
                await Task.Delay(50, ct);
            }
        }

        public async void Start() {
            task = Task.Run(async () => {
                cts?.Dispose();
                cts = new CancellationTokenSource();
                Dictionary<string, object> values = new Dictionary<string, object>();
                try {
                    do {
                        cts.Token.ThrowIfCancellationRequested();
                        var sw = Stopwatch.StartNew();

                        values = GetValuesFunc();

                        Progress.Report(values);

                        LastUpdate = DateTime.UtcNow;

                        await CoreUtil.Delay(
                            TimeSpan.FromSeconds(
                                Math.Max(1, Interval - sw.Elapsed.TotalSeconds)
                            ), cts.Token
                        );
                    } while (true);
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