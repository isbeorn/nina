#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using NINA.Sequencer.Trigger;
using Newtonsoft.Json.Linq;
using NINA.Core.Utility;

namespace NINA.Sequencer.Serialization {

    public class SequenceTriggerCreationConverter : JsonCreationConverter<ISequenceTrigger> {
        private ISequencerFactory factory;

        public SequenceTriggerCreationConverter(ISequencerFactory factory) {
            this.factory = factory;
        }

        public override ISequenceTrigger Create(Type objectType, JObject jObject) {
            if (jObject.TryGetValue("$type", out var token)) {
                var t = GetType(jObject.GetValue("$type").ToString());
                if (t == null) {
                    return new UnknownSequenceTrigger(token?.ToString());
                }
                try {
                    var method = factory.GetType().GetMethod(nameof(factory.GetTrigger)).MakeGenericMethod(new Type[] { t });
                    var obj = method.Invoke(factory, null);
                    if (obj == null) {
                        Logger.Error($"Encountered unknown sequence trigger: {token?.ToString()}");
                        return new UnknownSequenceTrigger(token?.ToString());
                    }
                    return (ISequenceTrigger)obj;
                } catch (Exception e) {
                    Logger.Error($"Encountered unknown sequence trigger: {token?.ToString()}", e);
                    return new UnknownSequenceTrigger(token?.ToString());
                }
            } else {
                return new UnknownSequenceTrigger(token?.ToString());
            }
        }
    }
}