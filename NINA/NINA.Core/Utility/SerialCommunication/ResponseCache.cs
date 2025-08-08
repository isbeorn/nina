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
using System.Collections.Generic;

namespace NINA.Core.Utility.SerialCommunication {

    public class ResponseCache {
        private readonly Dictionary<(Type, Type), (Response response, DateTime expires)> _cache;

        public ResponseCache() {
            _cache = new Dictionary<(Type, Type), (Response, DateTime)>();
        }

        public bool HasValidResponse(Type commandType, Type responseType) {
            if (commandType == null || responseType == null) return false;
            return _cache.ContainsKey((commandType, responseType)) && _cache[(commandType, responseType)].expires >= DateTime.UtcNow;
        }

        public void Add(ISerialCommand command, Response response) {
            if (command == null || response == null || response.Ttl == 0) return;
            if (_cache.ContainsKey((command.GetType(), response.GetType()))) {
                _cache[(command.GetType(), response.GetType())] = (response, DateTime.UtcNow.AddMilliseconds(response.Ttl));
            } else {
                _cache.Add((command.GetType(), response.GetType()), (response, DateTime.UtcNow.AddMilliseconds(response.Ttl)));
            }
        }

        public Response Get(Type commandType, Type responseType) {
            return HasValidResponse(commandType, responseType) ? _cache[(commandType, responseType)].response : null;
        }

        public void Clear() {
            _cache.Clear();
        }
    }
}