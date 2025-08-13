namespace NINA.Utility {
#nullable enable
    public interface ICommandLineOptions {
        string? ProfileId { get; }
        string? SequenceFile { get; }
        bool RunSequence { get; }
        bool ExitAfterSequence { get; }
        bool HasErrors { get; }
        bool Debug { get; }
    }
}
