using System;
using System.Collections.Generic;

namespace NINA.Utility.SerialCommunication {

    public class ResponseCache {
        private readonly Dictionary<Type, (Response response, DateTime expires)> _cache;

        public ResponseCache() {
            _cache = new Dictionary<Type, (Response, DateTime)>();
        }

        public bool HasValidResponse(Type commandType) {
            if (commandType == null) return false;
            return _cache.ContainsKey(commandType) && _cache[commandType].expires >= DateTime.UtcNow;
        }

        public void Add(ICommand command, Response response) {
            if (command == null || response == null || response.Ttl == 0) return;
            if (_cache.ContainsKey(command.GetType())) {
                _cache[command.GetType()] = (response, DateTime.UtcNow.AddMilliseconds(response.Ttl));
            } else {
                _cache.Add(command.GetType(), (response, DateTime.UtcNow.AddMilliseconds(response.Ttl)));
            }
        }

        public Response Get(Type commandType) {
            return HasValidResponse(commandType) ? _cache[commandType].response : null;
        }
    }
}