#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json.Linq;
using NINA.Sequencer.Utility.DateTimeProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Serialization {

    public class SequenceDateTimeProviderCreationConverter : JsonCreationConverter<IDateTimeProvider> {
        private IList<IDateTimeProvider> dateTimeProviders;

        public SequenceDateTimeProviderCreationConverter(IList<IDateTimeProvider> dateTimeProviders) {
            this.dateTimeProviders = dateTimeProviders;
        }

        public override IDateTimeProvider Create(Type objectType, JObject jObject) {
            var t = GetType(jObject.GetValue("$type").ToString());
            return this.dateTimeProviders.FirstOrDefault(x => x.GetType() == t);
        }
    }
}