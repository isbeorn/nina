#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Core.Utility.SerialCommunication;
using NUnit.Framework;

namespace NINA.Test.SerialCommunication {

    [TestFixture]
    internal class ResponseCacheTest {

        internal class CachableResponse : Response {
            public override int Ttl => 500;
        }

        internal class CachableSubclassResponse : CachableResponse {
        }

        internal class NonCachableResponse : Response {
            public override int Ttl => 0;
        }

        private Mock<ISerialCommand> _mockCommand;
        private CachableResponse _cachableResponse;
        private CachableResponse _cachableResponse2;
        private NonCachableResponse _nonCachableResponse;
        private CachableSubclassResponse _cachableSubclassResponse;
        private ResponseCache _sut;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _mockCommand = new Mock<ISerialCommand>();
        }

        [SetUp]
        public void Init() {
            _mockCommand.Reset();
            _sut = new ResponseCache();
            _cachableResponse = new CachableResponse();
            _cachableResponse2 = new CachableResponse();
            _nonCachableResponse = new NonCachableResponse();
            _cachableSubclassResponse = new CachableSubclassResponse();
        }

        [Test]
        public void TestAddNewCacheableResponse() {
            _sut.Add(_mockCommand.Object, _cachableResponse);
            var result = _sut.Get(_mockCommand.Object.GetType(), _cachableResponse.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType(), _cachableResponse.GetType()), Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(_cachableResponse));
        }

        [Test]
        public void TestAddOverExistingCacheableResponse() {
            _sut.Add(_mockCommand.Object, _cachableResponse);
            _sut.Add(_mockCommand.Object, _cachableResponse2);
            var result = _sut.Get(_mockCommand.Object.GetType(), _cachableResponse.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType(), _cachableResponse.GetType()), Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(_cachableResponse2));
        }

        [Test]
        public void TestAddNewNonCacheableResponse() {
            _sut.Add(_mockCommand.Object, _nonCachableResponse);
            var result = _sut.Get(_mockCommand.Object.GetType(), _nonCachableResponse.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType(), _nonCachableResponse.GetType()), Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestAddDifferentResponseTypesForSameCommandType() {
            _sut.Add(_mockCommand.Object, _cachableResponse);
            var result = _sut.Get(_mockCommand.Object.GetType(), _cachableSubclassResponse.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType(), _cachableResponse.GetType()), Is.True);
            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType(), _cachableSubclassResponse.GetType()), Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestNullInputs() {
            Assert.That(_sut.HasValidResponse(null, null), Is.False);
            Assert.That(_sut.Get(null, null), Is.Null);
            //below should not throw an exception
            _sut.Add(null, _cachableResponse);
            _sut.Add(_mockCommand.Object, null);
        }
    }
}