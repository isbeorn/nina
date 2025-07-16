using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Utility.DateTimeProvider {
    public class TimeProviderException : Exception {
        public TimeProviderException(string message, string localizedMessage) : base(message) {
            LocalizedMessage = localizedMessage;
        }

        public string LocalizedMessage { get; }
    }
}
