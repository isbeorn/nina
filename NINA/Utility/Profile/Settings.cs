using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class Settings : BaseINPC, ISettings {
    }
}