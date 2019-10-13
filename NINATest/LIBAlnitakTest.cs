using System;
using NUnit.Framework;
using AlnitakAstrosystemsSDK;

namespace NINATest {

    [TestFixture]
    public class LIBAlnitakTest {

        [Test]
        public void TestPing() {
            LIBAlnitak.Ping();
        }
    }
}