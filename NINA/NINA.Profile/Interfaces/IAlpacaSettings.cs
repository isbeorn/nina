using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Profile.Interfaces {
    public interface IAlpacaSettings : ISettings {
        int NumberOfPolls { get; set; }
        int PollInterval { get; set; }
        int DiscoveryPort { get; set; }
        double DiscoveryDuration { get; set; }
        bool ResolveDnsName{ get; set; }
        bool UseIPv4 { get; set; }
        bool UseIPv6 { get; set; }
        bool UseHttps { get; set; }
    }
}
