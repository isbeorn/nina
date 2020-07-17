#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;

namespace NINATest.SerialCommunication {

    [TestFixture]
    internal class ResponseCacheTest {
        private Mock<ICommand> _mockCommand;
        private Mock<Response> _mockResponse;
        private Mock<Response> _mockResponse2;
        private ResponseCache _sut;

        [SetUp]
        public void Init() {
            _mockCommand = new Mock<ICommand>();
            _mockResponse = new Mock<Response>();
            _mockResponse2 = new Mock<Response>();
            _sut = new ResponseCache();
        }

        [Test]
        public void TestAddNewCacheableResponse() {
            _mockResponse.Setup(m => m.Ttl).Returns(50);

            _sut.Add(_mockCommand.Object, _mockResponse.Object);
            var result = _sut.Get(_mockCommand.Object.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType()), Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(_mockResponse.Object));
        }

        [Test]
        public void TestAddOverExistingCacheableResponse() {
            _mockResponse.Setup(m => m.Ttl).Returns(50);
            _mockResponse2.Setup(m => m.Ttl).Returns(50);

            _sut.Add(_mockCommand.Object, _mockResponse.Object);
            _sut.Add(_mockCommand.Object, _mockResponse2.Object);
            var result = _sut.Get(_mockCommand.Object.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType()), Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(_mockResponse2.Object));
        }

        [Test]
        public void TestAddNewNonCacheableResponse() {
            _mockResponse.Setup(m => m.Ttl).Returns(0);

            _sut.Add(_mockCommand.Object, _mockResponse.Object);
            var result = _sut.Get(_mockCommand.Object.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType()), Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestNullInputs() {
            Assert.That(_sut.HasValidResponse(null), Is.False);
            Assert.That(_sut.Get(null), Is.Null);
            //below should not throw an exception
            _sut.Add(null, _mockResponse.Object);
            _sut.Add(_mockCommand.Object, null);
        }
    }
}