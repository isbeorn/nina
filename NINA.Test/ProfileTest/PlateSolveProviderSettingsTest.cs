using FluentAssertions;
using NINA.Core.Enum;
using NINA.Profile;
using NUnit.Framework;
using System.Runtime.Serialization;
using System.Xml.Linq;
using NINA.Profile.Interfaces;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    public class PlateSolveProviderSettingsTest {

        [Test]
        public void DefaultsAndOldProfileWithoutProviderMembers_UseBuiltinSelections() {
            var settings = new PlateSolveSettings();
            settings.PlateSolverProviderId.Should().BeNull();
            settings.BlindSolverProviderId.Should().BeNull();
            var serializer = new DataContractSerializer(typeof(PlateSolveSettings));
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, settings);
            stream.Position = 0;
            var document = XDocument.Load(stream);
            document.Descendants().Where(e => e.Name.LocalName is "PlateSolverProviderId" or "BlindSolverProviderId").Remove();
            using var reader = document.CreateReader();

            var restored = (PlateSolveSettings)serializer.ReadObject(reader)!;

            restored.PlateSolverProviderId.Should().BeNull();
            restored.BlindSolverProviderId.Should().BeNull();
            restored.PlateSolverType.Should().Be(PlateSolverEnum.ASTAP);
            restored.BlindSolverType.Should().Be(BlindSolverEnum.ASTAP);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void EnumAssignment_ClearsOnlyItsOverrideAndPreservesEnumNotificationBehavior(bool blind, bool changed) {
            var settings = new PlateSolveSettings();
            IPlateSolveSettings selections = settings;
            selections.PlateSolverProviderId = "primary";
            selections.BlindSolverProviderId = "blind";
            var notifications = new List<string?>();
            settings.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

            if (blind) {
                settings.BlindSolverType = changed ? BlindSolverEnum.LOCAL : settings.BlindSolverType;
            } else {
                settings.PlateSolverType = changed ? PlateSolverEnum.LOCAL : settings.PlateSolverType;
            }

            selections.PlateSolverProviderId.Should().Be(blind ? "primary" : null);
            selections.BlindSolverProviderId.Should().Be(blind ? null : "blind");
            notifications.Should().ContainSingle(x => x == (blind ? "BlindSolverProviderId" : "PlateSolverProviderId"));
            notifications.Count(x => x == (blind ? "BlindSolverType" : "PlateSolverType")).Should().Be(changed ? 1 : 0);
        }
    }
}
