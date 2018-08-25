using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Profile {

    public interface IRotatorSettings : ISettings {
        string Id { get; set; }
    }
}