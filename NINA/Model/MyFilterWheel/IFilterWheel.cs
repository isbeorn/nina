using NINA.Utility;
using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NINA.Model.MyFilterWheel {

    internal interface IFilterWheel : IDevice {
        short InterfaceVersion { get; }
        int[] FocusOffsets { get; }
        string[] Names { get; }
        short Position { get; set; }
        ArrayList SupportedActions { get; }
        AsyncObservableCollection<FilterInfo> Filters { get; }
    }
}