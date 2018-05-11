using System;

namespace NINA.Utility.Profile {
    public interface ISequenceSettings {
        TimeSpan EstimatedDownloadTime { get; set; }
        string TemplatePath { get; set; }
        long TimeSpanInTicks { get; set; }
    }
}