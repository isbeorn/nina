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
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Interfaces.Utility {

    public interface IMicroCache<T> {

        bool Contains(string key);

        T GetOrAdd(string key, Func<T> loadFunction, Func<CacheItemPolicy> getCacheItemPolicyFunction);

        T GetOrAdd(string key, Func<T> loadFunction, TimeSpan timeToLive);

        void Remove(string key);
    }

    public interface IMicroCacheFactory {

        IMicroCache<T> Create<T>();

        IMicroCache<T> Create<T>(ObjectCache objectCache);
    }
}