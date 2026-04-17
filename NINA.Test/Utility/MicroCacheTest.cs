#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Utility {

    [TestFixture]
    public class MicroCacheTest {

        /// <summary>
        /// Verifies that the cache rejects a null ObjectCache dependency instead of failing later during a lookup.
        /// </summary>
        [Test]
        public void Constructor_NullObjectCache_ThrowsArgumentNullException() {
            Assert.Throws<ArgumentNullException>(() => new MicroCache<int>(null));
        }

        /// <summary>
        /// Verifies the happy path where a value is loaded once, retained under its key, and returned from cache on later reads.
        /// </summary>
        [Test]
        public void GetOrAdd_ValueAlreadyCached_DoesNotInvokeLoaderAgain() {
            using MemoryCache objectCache = new MemoryCache(Guid.NewGuid().ToString());
            MicroCache<int> cache = new MicroCache<int>(objectCache);
            int loadCount = 0;

            int first = cache.GetOrAdd("gain", () => {
                loadCount++;
                return 120;
            }, TimeSpan.FromMinutes(1));
            int second = cache.GetOrAdd("gain", () => {
                loadCount++;
                return 999;
            }, TimeSpan.FromMinutes(1));

            Assert.That(first, Is.EqualTo(120));
            Assert.That(second, Is.EqualTo(120));
            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(cache.Contains("gain"), Is.True);
        }

        /// <summary>
        /// Verifies that removing a key invalidates the stored Lazy value and allows a later call to load fresh data.
        /// </summary>
        [Test]
        public void Remove_ExistingKey_AllowsFreshLoad() {
            using MemoryCache objectCache = new MemoryCache(Guid.NewGuid().ToString());
            MicroCache<string> cache = new MicroCache<string>(objectCache);

            string first = cache.GetOrAdd("camera", () => "ASI2600", TimeSpan.FromMinutes(1));
            cache.Remove("camera");
            string second = cache.GetOrAdd("camera", () => "QHY268", TimeSpan.FromMinutes(1));

            Assert.That(first, Is.EqualTo("ASI2600"));
            Assert.That(second, Is.EqualTo("QHY268"));
        }

        /// <summary>
        /// Verifies the critical concurrency invariant: competing readers for the same key share one loader result.
        /// </summary>
        [Test]
        public async Task GetOrAdd_ConcurrentReaders_InvokeLoaderOnlyOnce() {
            using MemoryCache objectCache = new MemoryCache(Guid.NewGuid().ToString());
            MicroCache<int> cache = new MicroCache<int>(objectCache);
            using ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
            int loadCount = 0;

            Task<int>[] tasks = Enumerable.Range(0, 16)
                .Select(_ => Task.Run(() => cache.GetOrAdd("shared", () => {
                    Interlocked.Increment(ref loadCount);
                    Assert.That(releaseLoader.Wait(TimeSpan.FromSeconds(2)), Is.True);
                    return 314;
                }, TimeSpan.FromMinutes(1))))
                .ToArray();

            Assert.That(SpinWait.SpinUntil(() => Volatile.Read(ref loadCount) == 1, TimeSpan.FromSeconds(2)), Is.True);
            releaseLoader.Set();
            int[] results = await Task.WhenAll(tasks);

            Assert.That(results, Is.All.EqualTo(314));
            Assert.That(loadCount, Is.EqualTo(1));
        }
    }
}
