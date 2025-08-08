using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Profile.Interfaces {
    public interface IDockPanelSettings : ISettings {
        bool CameraInfoOnly { get; set; }
        bool FilterWheelInfoOnly { get; set; }
        bool FocuserInfoOnly { get; set; }
        bool RotatorInfoOnly { get; set; }
        bool SwitchInfoOnly { get; set; }
        bool FlatDeviceInfoOnly { get; set; }
        bool ShowImagingHistogram { get; set; }
        bool IsLocked { get; set; }
    }
}
