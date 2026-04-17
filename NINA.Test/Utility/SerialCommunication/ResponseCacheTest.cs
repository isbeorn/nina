#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility.SerialCommunication;
using NUnit.Framework;
using System;

namespace NINA.Test.Utility.SerialCommunication {

    [TestFixture]
    public class ResponseCacheTest {

        /// <summary>
        /// Verifies that valid command/response pairs are cached by concrete types and returned while their TTL is active.
        /// </summary>
        [Test]
        public void AddAndGet_ValidResponseWithinTtl_ReturnsSameResponseInstance() {
            ResponseCache cache = new ResponseCache();
            TestCommand command = new TestCommand();
            CacheableResponse response = new CacheableResponse { DeviceResponse = "OK" };

            cache.Add(command, response);

            Assert.That(cache.HasValidResponse(typeof(TestCommand), typeof(CacheableResponse)), Is.True);
            Assert.That(cache.Get(typeof(TestCommand), typeof(CacheableResponse)), Is.SameAs(response));
        }

        /// <summary>
        /// Verifies null guards and zero-TTL responses are ignored so invalid cache entries cannot be retrieved later.
        /// </summary>
        [Test]
        public void Add_NullOrZeroTtlInput_DoesNotCreateCacheEntry() {
            ResponseCache cache = new ResponseCache();
            TestCommand command = new TestCommand();

            cache.Add(null, new CacheableResponse { DeviceResponse = "OK" });
            cache.Add(command, null);
            cache.Add(command, new ZeroTtlResponse { DeviceResponse = "OK" });

            Assert.That(cache.HasValidResponse(null, typeof(CacheableResponse)), Is.False);
            Assert.That(cache.HasValidResponse(typeof(TestCommand), null), Is.False);
            Assert.That(cache.Get(typeof(TestCommand), typeof(ZeroTtlResponse)), Is.Null);
        }

        /// <summary>
        /// Verifies that an expired response is treated as absent without relying on sleeps or wall-clock races.
        /// </summary>
        [Test]
        public void Get_NegativeTtlResponse_ReturnsNullBecauseEntryIsExpired() {
            ResponseCache cache = new ResponseCache();
            TestCommand command = new TestCommand();
            ExpiredResponse response = new ExpiredResponse { DeviceResponse = "STALE" };

            cache.Add(command, response);

            Assert.That(cache.HasValidResponse(typeof(TestCommand), typeof(ExpiredResponse)), Is.False);
            Assert.That(cache.Get(typeof(TestCommand), typeof(ExpiredResponse)), Is.Null);
        }

        /// <summary>
        /// Verifies that adding a second response for the same command/response type replaces the older value.
        /// </summary>
        [Test]
        public void Add_SameCommandAndResponseType_ReplacesStoredResponse() {
            ResponseCache cache = new ResponseCache();
            TestCommand command = new TestCommand();
            CacheableResponse first = new CacheableResponse { DeviceResponse = "OLD" };
            CacheableResponse second = new CacheableResponse { DeviceResponse = "NEW" };

            cache.Add(command, first);
            cache.Add(command, second);

            Assert.That(cache.Get(typeof(TestCommand), typeof(CacheableResponse)), Is.SameAs(second));
        }

        /// <summary>
        /// Verifies that clearing the cache removes otherwise-valid entries.
        /// </summary>
        [Test]
        public void Clear_ValidEntry_RemovesEntry() {
            ResponseCache cache = new ResponseCache();
            cache.Add(new TestCommand(), new CacheableResponse { DeviceResponse = "OK" });

            cache.Clear();

            Assert.That(cache.Get(typeof(TestCommand), typeof(CacheableResponse)), Is.Null);
        }

        /// <summary>
        /// Verifies the base response invariant that null or empty device replies are rejected during parsing.
        /// </summary>
        [Test]
        public void DeviceResponse_NullOrEmpty_ThrowsInvalidDeviceResponseException() {
            Assert.Throws<InvalidDeviceResponseException>(() => new CacheableResponse { DeviceResponse = null });
            Assert.Throws<InvalidDeviceResponseException>(() => new CacheableResponse { DeviceResponse = string.Empty });
        }

        private sealed class TestCommand : ISerialCommand {
            public string CommandString => "TEST";
            public bool HasResponse => true;
        }

        private class CacheableResponse : Response {
            public override int Ttl => 5000;
        }

        private sealed class ZeroTtlResponse : Response {
            public override int Ttl => 0;
        }

        private sealed class ExpiredResponse : Response {
            public override int Ttl => -1;
        }
    }
}
