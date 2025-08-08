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
        IDeviceUpdateTimer Create(Func<Dictionary<string, object>> getValuesFunc, Action<Dictionary<string, object>> updateValuesFunc, double interval, string context);
    }

    public class DefaultDeviceUpateTimerFactory : IDeviceUpdateTimerFactory {

        public DefaultDeviceUpateTimerFactory() {
        }

        public IDeviceUpdateTimer Create(Func<Dictionary<string, object>> getValuesFunc, Action<Dictionary<string, object>> updateValuesFunc, double interval) {
            return new DeviceUpdateTimer(getValuesFunc, updateValuesFunc, interval);
        }

        public IDeviceUpdateTimer Create(Func<Dictionary<string, object>> getValuesFunc, Action<Dictionary<string, object>> updateValuesFunc, double interval, string context) {
            return new DeviceUpdateTimer(getValuesFunc, updateValuesFunc, interval, context);
        }
    }

    public interface IDeviceUpdateTimer {
        Func<Dictionary<string, object>> GetValuesFunc { get; }
        public Action<Dictionary<string, object>> UpdateValuesFunc { get; }
        double Interval { get; set; }

        Task Stop();
        Task Run();
        Task WaitForNextUpdate(CancellationToken ct);
    }

    public class DeviceUpdateTimer : IDeviceUpdateTimer {
        public DeviceUpdateTimer(Func<Dictionary<string, object>> getValuesFunc, Action<Dictionary<string, object>> updateValuesFunc, double interval) : this(getValuesFunc, updateValuesFunc, interval, "Device") {
        }

        public DeviceUpdateTimer(Func<Dictionary<string, object>> getValuesFunc, Action<Dictionary<string, object>> updateValuesFunc, double interval, string context) {
            GetValuesFunc = getValuesFunc;
            Interval = interval;
            UpdateValuesFunc = updateValuesFunc;
            Context = context;
        }

        private CancellationTokenSource cts;
        private Task task;
        public Func<Dictionary<string, object>> GetValuesFunc { get; private set; }
        public Action<Dictionary<string, object>> UpdateValuesFunc { get; private set; }
        public string Context { get; }
        public DateTimeOffset LastUpdate { get; private set; } = DateTimeOffset.MinValue;
        public double Interval { get; set; }

        private DateTimeOffset? lastSlowLogTime = null;

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

        public async Task Run() {
            task = Task.Run(async () => {
                try { cts?.Dispose(); } catch { }
                cts = new CancellationTokenSource();
                var token = cts.Token;

                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Interval));

                Dictionary<string, object> values = new Dictionary<string, object>();
                try {
                    while (await timer.WaitForNextTickAsync(token) && !token.IsCancellationRequested) {
                        var getStart = DateTimeOffset.UtcNow;                        
                        values = GetValuesFunc();

                        var updateStart = DateTimeOffset.UtcNow;                        
                        UpdateValuesFunc(values);

                        var updateEnd = DateTimeOffset.UtcNow;
                        var totalDuration = updateEnd - getStart;
                        if (Logger.IsEnabled(Enum.LogLevelEnum.TRACE)) {
                            Logger.Trace($"{Context} values have been updated. Poll start: {getStart:o}; Update start: {updateStart:o}; Update end: {updateEnd:o}; Poll duration {updateStart - getStart}; Update duration {updateEnd - updateStart}; Overall duration {totalDuration}");
                        }

                        if (totalDuration.TotalSeconds > Interval) {
                            var now = DateTimeOffset.UtcNow;
                            if (!lastSlowLogTime.HasValue || now - lastSlowLogTime > TimeSpan.FromMinutes(5)) {
                                Logger.Warning($"{Context} value update cycle took longer than the device poll interval (Total: {totalDuration.TotalSeconds:F2}s > {Interval}s; Poll: {updateStart - getStart}; Update: {updateEnd - updateStart})");
                                lastSlowLogTime = now;
                            }
                        }
                        LastUpdate = updateEnd;
                    }
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                } finally {
                    values.Clear();
                    values.Add("Connected", false);
                    UpdateValuesFunc(values);
                }
            });
            await task;
        }
    }
}