using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Media;
using ProfileModel = NINA.Profile.Profile;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    public class PluginOptionsAccessorGeneratedTypesTest {
        private Guid pluginGuid;
        private PluginSettings pluginSettings;
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void SetUp() {
            pluginGuid = Guid.Parse("85649312-1621-4c4a-936a-b4ce3e9d70a0");
            pluginSettings = new PluginSettings();
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(profileService => profileService.ActiveProfile.PluginSettings).Returns(pluginSettings);
        }

        /// <summary>
        /// Verifies that the generated plugin options accessor round-trips every primitive type against the active profile.
        /// </summary>
        [Test]
        public void SetAndGetValue_RoundTripsEveryGeneratedAccessorPrimitiveType() {
            IPluginOptionsAccessor accessor = CreateAccessor();
            DateTime timestamp = new DateTime(2026, 4, 17, 22, 0, 0, DateTimeKind.Utc);
            Guid storedGuid = Guid.Parse("1197f3eb-ff78-4ff4-8915-aa4c36a11f81");

            accessor.SetValueBoolean("bool", true);
            accessor.SetValueByte("byte", 250);
            accessor.SetValueSByte("sbyte", -100);
            accessor.SetValueChar("char", 'P');
            accessor.SetValueDecimal("decimal", 19.875m);
            accessor.SetValueDouble("double", 21.5d);
            accessor.SetValueSingle("single", 7.25f);
            accessor.SetValueInt32("int", -42);
            accessor.SetValueUInt32("uint", 42u);
            accessor.SetValueInt64("long", -42000000000L);
            accessor.SetValueUInt64("ulong", 42000000000UL);
            accessor.SetValueInt16("short", -16);
            accessor.SetValueUInt16("ushort", 16);
            accessor.SetValueString("string", "accessor value");
            accessor.SetValueDateTime("datetime", timestamp);
            accessor.SetValueGuid("guid", storedGuid);

            accessor.GetValueBoolean("bool", false).Should().BeTrue();
            accessor.GetValueByte("byte", 1).Should().Be(250);
            accessor.GetValueSByte("sbyte", 1).Should().Be(-100);
            accessor.GetValueChar("char", 'x').Should().Be('P');
            accessor.GetValueDecimal("decimal", 1m).Should().Be(19.875m);
            accessor.GetValueDouble("double", 1d).Should().Be(21.5d);
            accessor.GetValueSingle("single", 1f).Should().Be(7.25f);
            accessor.GetValueInt32("int", 1).Should().Be(-42);
            accessor.GetValueUInt32("uint", 1u).Should().Be(42u);
            accessor.GetValueInt64("long", 1L).Should().Be(-42000000000L);
            accessor.GetValueUInt64("ulong", 1UL).Should().Be(42000000000UL);
            accessor.GetValueInt16("short", 1).Should().Be(-16);
            accessor.GetValueUInt16("ushort", 1).Should().Be(16);
            accessor.GetValueString("string", "default").Should().Be("accessor value");
            accessor.GetValueDateTime("datetime", DateTime.MinValue).Should().Be(timestamp);
            accessor.GetValueGuid("guid", Guid.Empty).Should().Be(storedGuid);
        }

        /// <summary>
        /// Verifies that color options preserve ARGB channel values and fall back to defaults when the stored value is not an integer color.
        /// </summary>
        [Test]
        public void ColorOptions_RoundTripArgbAndFallbackWhenStoredTypeDoesNotMatch() {
            IPluginOptionsAccessor accessor = CreateAccessor();
            Color expected = Color.FromArgb(128, 10, 20, 30);
            Color fallback = Color.FromArgb(255, 1, 2, 3);

            accessor.SetValueColor("guideColor", expected);
            Color actual = accessor.GetValueColor("guideColor", fallback);
            pluginSettings.SetValue(pluginGuid, "badGuideColor", "not a color");
            Color badActual = accessor.GetValueColor("badGuideColor", fallback);

            actual.Should().Be(expected);
            badActual.Should().Be(fallback);
        }

        /// <summary>
        /// Verifies that enum options are stored by name and invalid persisted names return the caller-provided default.
        /// </summary>
        [Test]
        public void EnumOptions_RoundTripNamesAndFallbackForInvalidPersistedName() {
            IPluginOptionsAccessor accessor = CreateAccessor();

            accessor.SetValueEnum("fileType", FileTypeEnum.XISF);
            pluginSettings.SetValue(pluginGuid, "invalidFileType", "DefinitelyNotAFileType");

            accessor.GetValueEnum("fileType", FileTypeEnum.FITS).Should().Be(FileTypeEnum.XISF);
            accessor.GetValueEnum("missingFileType", FileTypeEnum.FITS).Should().Be(FileTypeEnum.FITS);
            accessor.GetValueEnum("invalidFileType", FileTypeEnum.FITS).Should().Be(FileTypeEnum.FITS);
        }

        /// <summary>
        /// Verifies that assembly GUID discovery returns the persisted NINA.Profile assembly GUID and null for assemblies without one.
        /// </summary>
        [Test]
        public void GetAssemblyGuid_ReturnsGuidOnlyWhenAssemblyDeclaresOne() {
            Guid? profileAssemblyGuid = PluginOptionsAccessor.GetAssemblyGuid(typeof(ProfileModel));
            AssemblyName assemblyName = new AssemblyName("NoGuidDynamicPluginAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("NoGuidDynamicPluginType", TypeAttributes.Public);
            Type dynamicType = typeBuilder.CreateType();
            Guid? dynamicAssemblyGuid = PluginOptionsAccessor.GetAssemblyGuid(dynamicType);

            profileAssemblyGuid.Should().Be(Guid.Parse("8540150e-7ff0-4f7b-a714-0f6abdb1ac60"));
            dynamicAssemblyGuid.Should().BeNull();
        }

        private IPluginOptionsAccessor CreateAccessor() {
            return new PluginOptionsAccessor(profileServiceMock.Object, pluginGuid);
        }
    }
}
