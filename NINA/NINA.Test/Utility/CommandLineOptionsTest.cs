using FluentAssertions;
using NINA.Utility;

namespace NINA.Test.Utility {

    [TestFixture]
    public class CommandLineOptionsTest {

        [Test]
        public void TestAll() {
            string[] shortArgs = { "-p", "MYPROFILE", "-s", "MYSEQUENCE", "-r" };
            var sut = new CommandLineOptions(shortArgs);
            sut.ProfileId.Should().Be("MYPROFILE");
            sut.SequenceFile.Should().Be("MYSEQUENCE");
            sut.RunSequence.Should().BeTrue();
            sut.HasErrors.Should().BeFalse();

            string[] longArgs = { "--profileid", "MYPROFILE", "--sequencefile", "MYSEQUENCE", "--runsequence" };
            sut = new CommandLineOptions(longArgs);
            sut.ProfileId.Should().Be("MYPROFILE");
            sut.SequenceFile.Should().Be("MYSEQUENCE");
            sut.RunSequence.Should().BeTrue();
            sut.HasErrors.Should().BeFalse();

            string[] profileOnly = { "-p", "MYPROFILE"};
            sut = new CommandLineOptions(profileOnly);
            sut.ProfileId.Should().Be("MYPROFILE");
            sut.SequenceFile.Should().BeNull();
            sut.RunSequence.Should().BeFalse();
            sut.HasErrors.Should().BeFalse();
        }

        [Test]
        public void TestEmpty() {
            var sut = new CommandLineOptions(Array.Empty<string>());
            sut.ProfileId.Should().BeNull();
            sut.SequenceFile.Should().BeNull();
            sut.RunSequence.Should().BeFalse();
            sut.HasErrors.Should().BeFalse();
        }

        [Test]
        public void TestError() {
            string[] bad = { "-X", "foo" };
            var sut = new CommandLineOptions(bad);
            sut.ProfileId.Should().BeNull();
            sut.SequenceFile.Should().BeNull();
            sut.RunSequence.Should().BeFalse();
            sut.HasErrors.Should().BeTrue();
        }
    }
}
