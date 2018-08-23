using System;

namespace NINA.Utility.Profile {

    public interface ISequenceSettings : ISettings {
        TimeSpan EstimatedDownloadTime { get; set; }
        string TemplatePath { get; set; }
        long TimeSpanInTicks { get; set; }
    }
}