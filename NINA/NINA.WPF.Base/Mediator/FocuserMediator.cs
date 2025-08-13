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
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.WPF.Base.Mediator {

    public class FocuserMediator : DeviceMediator<IFocuserVM, IFocuserConsumer, FocuserInfo>, IFocuserMediator {

        public void ToggleTempComp(bool tempComp) {
            handler.ToggleTempComp(tempComp);
        }

        public Task<int> MoveFocuser(int position, CancellationToken ct) {
            return handler.MoveFocuser(position, ct);
        }

        public Task<int> MoveFocuserRelative(int position, CancellationToken ct) {
            return handler.MoveFocuserRelative(position, ct);
        }

        public Task<int> MoveFocuserByTemperatureRelative(double temperature, double slope, CancellationToken ct) {
            return handler.MoveFocuserByTemperatureRelative(temperature, slope, ct);
        }

        public void BroadcastSuccessfulAutoFocusRun(AutoFocusInfo info) {
            Logger.Info($"Autofocus notification received - Temperature {info.Temperature}");
            handler.SetFocusedTemperature(info.Temperature);
            List<IFocuserConsumer> receivers;
            lock (consumers) {
                receivers = new List<IFocuserConsumer>(consumers);
            }
            foreach (IFocuserConsumer c in receivers) {
                try {
                    c.UpdateEndAutoFocusRun(info);
                } catch (Exception e) {
                    Logger.Error(e);
                }
            }
        }

        public void BroadcastNewAutoFocusPoint(DataPoint dataPoint) {
            List<IFocuserConsumer> receivers;
            lock (consumers) {
                receivers = new List<IFocuserConsumer>(consumers);
            }
            foreach (IFocuserConsumer c in receivers) {
                try {
                    c.NewAutoFocusPoint(dataPoint);
                } catch (Exception e) {
                    Logger.Error(e);
                }
            }
        }

        public void BroadcastUserFocused(FocuserInfo info) {
            Logger.Info($"User Focused notification received - Temperature {info.Temperature}");
            handler.SetFocusedTemperature(info.Temperature);
            List<IFocuserConsumer> receivers;
            lock (consumers) {
                receivers = new List<IFocuserConsumer>(consumers);
            }
            foreach (IFocuserConsumer c in receivers) {
                try {
                    c.UpdateUserFocused(info);
                } catch (Exception e) {
                    Logger.Error(e);
                }
            }
        }

        public void BroadcastAutoFocusRunStarting() {
            Logger.Info($"Autofocus starting notification received");
            List<IFocuserConsumer> receivers;
            lock (consumers) {
                receivers = new List<IFocuserConsumer>(consumers);
            }
            foreach (IFocuserConsumer c in receivers) {
                try {
                    c.AutoFocusRunStarting();
                } catch (Exception e) {
                    Logger.Error(e);
                }
            }
        }
    }
}