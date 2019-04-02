#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using OxyPlot.Axes;
using System;
using System.Timers;

namespace NINA.Utility {

    public class Ticker : BaseINPC {

        public Ticker(double interval) {
            _timer = new Timer();
            _timer.Interval = interval; // 1 second updates
            _timer.Elapsed += timer_Elapsed;
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

        private void timer_Elapsed(object sender, ElapsedEventArgs e) {
            RaisePropertyChanged(nameof(Now));
            RaisePropertyChanged(nameof(OxyNow));
        }

        public void Stop() {
            _timer.Stop();
        }
    }
}