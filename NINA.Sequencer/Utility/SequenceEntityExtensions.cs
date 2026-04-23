using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace NINA.Sequencer.Utility {
    /// <summary>
    /// Creating new instances of Sequence Entities doesn't add the Name, Category and Icon to them, as these are handled differently in the core application
    /// These Extension methods work similar to the plugin loader and inject the export attributes into the respective fields
    /// This should be put into a future Version of core NINA.Sequencer
    /// </summary>
    public static class SequenceEntityExtension {
        public static ISequenceContainer AddMetaData(this ISequenceContainer entity, IApplicationResourceDictionary resourceDictionary) {
            var attributes = entity.GetType().GetCustomAttributes(false).OfType<ExportMetadataAttribute>();
            entity.Description = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Description")?.Value?.ToString() ?? "");
            entity.Category = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Category")?.Value?.ToString() ?? "");
            entity.Icon = (System.Windows.Media.GeometryGroup)resourceDictionary[(attributes.FirstOrDefault(x => x.Name == "Icon")?.Value?.ToString() ?? "")];
            return entity;
        }
        public static ISequenceItem AddMetaData(this ISequenceItem entity, IApplicationResourceDictionary resourceDictionary) {
            var attributes = entity.GetType().GetCustomAttributes(false).OfType<ExportMetadataAttribute>();
            entity.Name = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Name")?.Value?.ToString() ?? "");
            entity.Description = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Description")?.Value?.ToString() ?? "");
            entity.Category = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Category")?.Value?.ToString() ?? "");
            entity.Icon = (System.Windows.Media.GeometryGroup)resourceDictionary[(attributes.FirstOrDefault(x => x.Name == "Icon")?.Value?.ToString() ?? "")];
            return entity;
        }
        public static ISequenceCondition AddMetaData(this ISequenceCondition entity, IApplicationResourceDictionary resourceDictionary) {
            var attributes = entity.GetType().GetCustomAttributes(false).OfType<ExportMetadataAttribute>();
            entity.Name = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Name")?.Value?.ToString() ?? "");
            entity.Description = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Description")?.Value?.ToString() ?? "");
            entity.Category = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Category")?.Value?.ToString() ?? "");
            entity.Icon = (System.Windows.Media.GeometryGroup)resourceDictionary[(attributes.FirstOrDefault(x => x.Name == "Icon")?.Value?.ToString() ?? "")];
            return entity;
        }
        public static ISequenceTrigger AddMetaData(this ISequenceTrigger entity, IApplicationResourceDictionary resourceDictionary) {
            var attributes = entity.GetType().GetCustomAttributes(false).OfType<ExportMetadataAttribute>();
            entity.Name = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Name")?.Value?.ToString() ?? "");
            entity.Description = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Description")?.Value?.ToString() ?? "");
            entity.Category = GrabLabel(attributes.FirstOrDefault(x => x.Name == "Category")?.Value?.ToString() ?? "");
            entity.Icon = (System.Windows.Media.GeometryGroup)resourceDictionary[(attributes.FirstOrDefault(x => x.Name == "Icon")?.Value?.ToString() ?? "")];
            return entity;
        }
        private static string GrabLabel(string label) {
            if (label == null) return "";
            if (label.StartsWith("Lbl_")) {
                return Loc.Instance[label];
            } else {
                return label;
            }
        }
    }
}
