#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NINA.Sequencer.Container;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace NINA.Sequencer.Serialization {

    public class SequenceJsonConverter {
        private static readonly IContractResolver ContractResolver = new SequenceContractResolver();
        private ISequencerFactory factory;

        private List<JsonConverter> converters;

        public SequenceJsonConverter(ISequencerFactory factory) {
            this.factory = factory;
            var c = new SequenceContainerCreationConverter(factory);
            this.converters = new List<JsonConverter>() {
                c,
                new SequenceItemCreationConverter(factory, c),
                new SequenceConditionCreationConverter(factory),
                new SequenceTriggerCreationConverter(factory),
                new SequenceDateTimeProviderCreationConverter(factory)
            };
        }

        public string Serialize(ISequenceContainer container) {
            var json = JsonConvert.SerializeObject(container, Formatting.Indented, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ContractResolver = ContractResolver
            });
            return json;
        }

        public ISequenceContainer Deserialize(string sequenceJSON) {
            return Deserialize(sequenceJSON, sourcePath: null);
        }

        public ISequenceContainer Deserialize(string sequenceJSON, string sourcePath) {
            var settings = new JsonSerializerSettings {
                Converters = converters,
                Context = new StreamingContext(StreamingContextStates.File, sourcePath)
            };

            return JsonConvert.DeserializeObject<ISequenceContainer>(sequenceJSON, settings);
        }

    }

    internal class SequenceContractResolver : DefaultContractResolver {

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (member.DeclaringType == typeof(SequenceContainer) && IsLinkedTemplateRuntimeState(property.UnderlyingName)) {
                Predicate<object> shouldSerialize = property.ShouldSerialize;
                property.ShouldSerialize = instance =>
                    instance is not LinkedTemplateContainer && (shouldSerialize == null || shouldSerialize(instance));
            }

            if (member.DeclaringType == typeof(LinkedTemplateContainer)
                && (property.UnderlyingName == nameof(LinkedTemplateContainer.LinkState)
                    || property.UnderlyingName == nameof(LinkedTemplateContainer.IsExpanded))) {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }

        private static bool IsLinkedTemplateRuntimeState(string propertyName) {
            return propertyName == nameof(SequenceContainer.Items)
                || propertyName == nameof(SequenceContainer.Conditions)
                || propertyName == nameof(SequenceContainer.Triggers)
                || propertyName == nameof(SequenceContainer.IsExpanded);
        }
    }
}
