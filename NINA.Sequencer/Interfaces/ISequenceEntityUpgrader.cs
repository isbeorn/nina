using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json;

namespace NINA.Sequencer {
    public interface ISequenceEntityUpgrader {
        // Pretty name
        string Name { get; set; }
        // Plugin assembly name
        string AssemblyName { get; set; }
        // This can be removed, but requires new nugets so hold off for now...
        bool CanUpgrade(SequenceUpgradeContext context, SequenceUpgradeStage stage);
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

    public enum SequenceUpgradeStage {
        BeforeCreate,
        AfterCreate,
        AfterPopulate
    }
}
