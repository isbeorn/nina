namespace NINA.Utility {
#nullable enable
    public interface ICommandLineOptions {
        string? ProfileId { get; }
        string? SequenceFile { get; }
        bool RunSequence { get; }
        bool HasErrors { get; }
    }
}
