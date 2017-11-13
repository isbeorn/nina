using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        void timer_Elapsed(object sender, ElapsedEventArgs e) {
            RaisePropertyChanged(nameof(Now));
            RaisePropertyChanged(nameof(OxyNow));
        }

        public void Stop() {
            _timer.Stop();
        }

    }
}
