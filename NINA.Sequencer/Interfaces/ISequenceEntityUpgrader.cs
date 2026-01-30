using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace NINA.Sequencer {
    public interface ISequenceEntityUpgrader : ISequenceEntity {
        string Name { get; }
        SequenceUpgradeStage Stage { get; }
        bool CanUpgrade(SequenceUpgradeContext context);

        object? Upgrade(SequenceUpgradeContext context, object? current);
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
