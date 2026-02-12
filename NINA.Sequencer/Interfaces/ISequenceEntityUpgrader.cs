using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json;

namespace NINA.Sequencer {
    public interface ISequenceEntityUpgrader {
        // Pretty name
        string Name { get; set; }
        // Plugin assembly name
        string AssemblyName { get; set; }
        // The SequenceUpgradeStages handled (OR'd together)
        SequenceUpgradeStage Stages { get; }
        object? Upgrade(SequenceUpgradeContext context, SequenceUpgradeStage stage, object? current);
    }
    public sealed class SequenceUpgradeContext {
        public required JsonSerializer Serializer { get; init; }
        public required Type RequestedType { get; init; }
        public required JObject Json { get; init; }
        public string? OriginalTypeString { get; init; }
        public object? ExistingValue { get; init; }
        public ISequencerFactory Factory { get; init; }
    }

    [Flags]
    public enum SequenceUpgradeStage {
        BeforeCreate = 1,
        Create = 2,
        AfterCreate = 4,
        AfterPopulate = 8
    }
}
