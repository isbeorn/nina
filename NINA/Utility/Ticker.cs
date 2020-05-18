#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OxyPlot.Axes;
using System;
using System.Timers;

namespace NINA.Utility {

    public class Ticker : BaseINPC {

        public Ticker(TimeSpan interval) {
            _timer = new Timer();
            _timer.Interval = interval.TotalMilliseconds;
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        private Timer _timer;

        public DateTime Now {
            get {
                return DateTime.Now;
            }
        }

        public double OxyNow {
            get {
                return DateTimeAxis.ToDouble(DateTime.Now);
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            RaisePropertyChanged(nameof(Now));
            RaisePropertyChanged(nameof(OxyNow));
        }

        public void Stop() {
            _timer.Stop();
        }
    }
}
