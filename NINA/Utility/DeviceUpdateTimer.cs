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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility {

    internal class DeviceUpdateTimer {

        public DeviceUpdateTimer(Func<Dictionary<string, object>> getValuesFunc, Action<Dictionary<string, object>> updateValuesFunc, double interval) {
            GetValuesFunc = getValuesFunc;
            Interval = interval;
            Progress = new Progress<Dictionary<string, object>>(updateValuesFunc);
        }

        private CancellationTokenSource cts;
        private Task task;
        public Func<Dictionary<string, object>> GetValuesFunc { get; private set; }
        public IProgress<Dictionary<string, object>> Progress { get; private set; }
        public double Interval { get; set; }

        public async Task Stop() {
            cts?.Cancel();
            do {
                await Task.Delay(100);
            } while (!task?.IsCompleted == true);
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

                        await Utility.Delay(
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