#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NINA.Sequencer.Editing {
    internal interface ISequenceEditDetails {
        string Details { get; }
    }

    // History captions are immutable strings captured at edit time, never live model bindings.
    internal static class SequenceEditDetails {
        public static string Name(ISequenceEntity entity) {
            string name = entity is ISequenceRootContainer root ? root.SequenceTitle : entity.Name;
            if (!string.IsNullOrWhiteSpace(name)) return name;
            string label = entity.GetType().GetCustomAttributes<ExportMetadataAttribute>(false)
                .FirstOrDefault(attribute => attribute.Name == "Name")?.Value as string;
            return LocalizedLabel(label) ?? TypeName(entity.GetType());
        }

        public static string Path(ISequenceEntity entity) {
            var names = new List<string>();
            var visited = new HashSet<ISequenceEntity>(ReferenceEqualityComparer.Instance);
            for (var current = entity; current != null && visited.Add(current); current = current.Parent) names.Add(Name(current));
            names.Reverse();
            return string.Join(" > ", names);
        }

        public static string Context(ISequenceEntity entity) {
            int index = entity switch {
                ISequenceCondition condition when entity.Parent is IConditionable parent => parent.Conditions.IndexOf(condition),
                ISequenceTrigger trigger when entity.Parent is ITriggerable parent => parent.Triggers.IndexOf(trigger),
                ISequenceItem item when entity.Parent != null => entity.Parent.Items.IndexOf(item),
                _ => -1
            };
            return index < 0 ? Path(entity) : $"{Path(entity)} (#{index + 1})";
        }

        public static string Field(IEnumerable<PropertyInfo> path, string member) {
            var parts = path.Select(property => property.Name).ToList();
            if (member != "Definition") parts.Add(member);
            return string.Join(" / ", parts.Select(part => Words(
                part.Length > 10 && part.EndsWith("Expression", StringComparison.Ordinal) ? part[..^10] : part)));
        }

        public static string Value(object value, string displayMemberPath = null) {
            if (value == null) return Loc.Instance["Lbl_SequenceHistory_NoValue"];
            string text;
            try {
                text = value switch {
                    string s => s,
                    bool b => Loc.Instance[b ? "LblTrue" : "LblFalse"],
                    Enum e => LocalizedLabel(e.GetType().GetField(e.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description) ?? Words(e.ToString()),
                    Type type => TypeName(type),
                    IFormattable f => f.ToString(null, CultureInfo.CurrentCulture),
                    _ => DisplayMember(value, displayMemberPath) ?? DisplayMember(value, "DisplayName")
                        ?? DisplayMember(value, "Name") ?? ObjectText(value)
                };
            } catch { text = TypeName(value.GetType()); }
            text = text?.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrEmpty(text)) return Loc.Instance["Lbl_SequenceHistory_NoValue"];
            return text.Length > 240 ? text[..240] + "..." : text;
        }

        private static string DisplayMember(object value, string path) {
            if (string.IsNullOrWhiteSpace(path)) return null;
            foreach (string member in path.Split('.')) {
                PropertyInfo property = value?.GetType().GetProperty(member);
                if (property?.CanRead != true || property.GetIndexParameters().Length != 0) return null;
                value = property.GetValue(value);
            }
            return value is string text && !string.IsNullOrWhiteSpace(text) ? text : null;
        }

        private static string ObjectText(object value) {
            Type type = value.GetType();
            string text = value.ToString();
            // Object.ToString returns a CLR type name. Preserve useful custom text and literal
            // string values, including expressions and paths which legitimately contain dots.
            return string.IsNullOrWhiteSpace(text) || text == type.FullName || text == type.AssemblyQualifiedName || text == type.Name
                ? TypeName(type) : text;
        }

        private static string TypeName(Type type) => Words(type.Name.Split('`')[0]);

        private static string Words(string identifier) => Regex.Replace(
            identifier.Replace('_', ' '), "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");

        private static string LocalizedLabel(string label) {
            if (string.IsNullOrWhiteSpace(label)) return null;
            if (!label.StartsWith("Lbl", StringComparison.Ordinal)) return label;
            string text = Loc.Instance[label];
            return text.StartsWith("MISSING LABEL ", StringComparison.Ordinal) ? null : text;
        }

        public static string Change(string context, string before, string after) =>
            Join(new[] { context, string.Format(Loc.Instance["Lbl_SequenceHistory_ValueChange"], before, after) });

        public static string Coordinates(Coordinates value, bool negativeDec = false) => value == null
            ? Loc.Instance["Lbl_SequenceHistory_NoValue"]
            : $"{value.RAString}, {(negativeDec && value.Dec == 0 ? "-" + value.DecString.TrimStart('+', '-') : value.DecString)} ({value.Epoch})";

        public static string Coordinates(SequenceCoordinateState value) => string.Format(Loc.Instance["Lbl_SequenceHistory_CoordinateValue"],
            Value(value.RaDefinition), Value(value.DecDefinition), Value(value.RotationDefinition), value.Value.Epoch);

        public static string Target(LinkedTemplateTargetOverride value) => value == null
            ? Loc.Instance["Lbl_SequenceHistory_NoValue"]
            : $"{Value(value.TargetName)}: {Coordinates(value.InputCoordinates?.Coordinates, value.InputCoordinates?.NegativeDec == true)}; {Value(value.PositionAngle)}°";

        public static string Join(IEnumerable<string> details) {
            string[] lines = details.Where(text => !string.IsNullOrWhiteSpace(text)).Distinct().ToArray();
            const int maximum = 5;
            var visible = lines.Take(maximum).ToList();
            if (lines.Length > maximum) visible.Add(string.Format(Loc.Instance["Lbl_SequenceHistory_MoreChanges"], lines.Length - maximum));
            return string.Join(Environment.NewLine, visible);
        }
    }
}