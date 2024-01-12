#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Serialization {

    public class SequenceJsonConverter {
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
                new SequenceDateTimeProviderCreationConverter(factory.DateTimeProviders)
            };
        }

        public string Serialize(ISequenceContainer container) {
            var json = JsonConvert.SerializeObject(container, Formatting.Indented, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.All
            });
            return json;
        }

        public ISequenceContainer Deserialize(string sequenceJSON) {
            var container = JsonConvert.DeserializeObject<ISequenceContainer>(sequenceJSON, new JsonSerializerSettings() {
                Converters = converters
            });

            return container;
        }
    }
}