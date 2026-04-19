#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Interfaces;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Utility;
using System.ComponentModel;
using System.Threading;

namespace NINA.Test.Utility {

    [TestFixture]
    public class PluggableIntegrationBehaviorTest {

        /// <summary>
        /// Verifies pluggable behavior selection starts with the NINA default, persists plugin selections, and falls back to the default for stale profile IDs.
        /// </summary>
        [Test]
        public void PluggableBehaviorSelector_SelectsPersistsAndFallsBackDeterministically() {
            ApplicationSettings settings = new ApplicationSettings();
            Mock<IProfileService> profileService = CreateProfileService(settings);
            var defaultBehavior = new TestBehavior("Default", "nina-default");
            var pluginBehavior = new TestBehavior("Plugin", "plugin-behavior");
            var sut = new PluggableBehaviorSelector<ITestBehavior, TestBehavior>(profileService.Object, defaultBehavior);
            var changed = new List<string>();
            sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            sut.SelectedBehavior.Should().BeSameAs(defaultBehavior);
            sut.AddBehavior(pluginBehavior);
            sut.SelectedBehavior = pluginBehavior;

            sut.Behaviors.Should().ContainInOrder(defaultBehavior, pluginBehavior);
            sut.SelectedBehavior.Should().BeSameAs(pluginBehavior);
            settings.SelectedPluggableBehaviors.Should().ContainSingle()
                .Which.Should().Be(new KeyValuePair<string, string>(typeof(ITestBehavior).FullName, pluginBehavior.ContentId));
            sut.GetBehavior("missing-plugin").Should().BeSameAs(defaultBehavior);
            changed.Should().Contain(nameof(PluggableBehaviorSelector<ITestBehavior, TestBehavior>.Behaviors));
            changed.Should().Contain(nameof(PluggableBehaviorSelector<ITestBehavior, TestBehavior>.SelectedBehavior));
        }

        /// <summary>
        /// Verifies pluggable behavior selectors reject null and wrong-type behavior registrations instead of adding null entries.
        /// </summary>
        [Test]
        public void PluggableBehaviorSelector_RejectsInvalidBehaviorRegistrations() {
            ApplicationSettings settings = new ApplicationSettings();
            Mock<IProfileService> profileService = CreateProfileService(settings);
            var defaultBehavior = new TestBehavior("Default", "nina-default");
            var sut = new PluggableBehaviorSelector<ITestBehavior, TestBehavior>(profileService.Object, defaultBehavior);

            Action addNull = () => sut.AddBehavior(null);
            Action addWrongType = () => sut.AddBehavior(new OtherBehavior());
            Action selectUnknown = () => sut.SelectedBehavior = new TestBehavior("Unknown", "unknown");

            addNull.Should().Throw<ArgumentException>().WithParameterName("behavior");
            addWrongType.Should().Throw<ArgumentException>().WithParameterName("behavior");
            selectUnknown.Should().Throw<ArgumentException>().WithParameterName("SelectedBehavior");
            sut.Behaviors.Should().ContainSingle().Which.Should().BeSameAs(defaultBehavior);
        }

        /// <summary>
        /// Verifies profile collection changes raise the selector event only when the selected pluggable behavior content ID changes.
        /// </summary>
        [Test]
        public void PluggableBehaviorSelector_RaisesSelectedBehaviorChangedForProfileSelectionChanges() {
            ApplicationSettings settings = new ApplicationSettings();
            Mock<IProfileService> profileService = CreateProfileService(settings);
            var sut = new PluggableBehaviorSelector<ITestBehavior, TestBehavior>(profileService.Object, new TestBehavior("Default", "nina-default"));
            int selectedChanges = 0;
            sut.SelectedBehaviorChanged += (_, _) => selectedChanges++;

            settings.SelectedPluggableBehaviors.Add(new KeyValuePair<string, string>(typeof(ITestBehavior).FullName, "plugin-a"));
            settings.SelectedPluggableBehaviors.Clear();
            settings.SelectedPluggableBehaviors.Add(new KeyValuePair<string, string>(typeof(ITestBehavior).FullName, "plugin-a"));

            selectedChanges.Should().Be(3);
        }

        /// <summary>
        /// Verifies plugin equipment providers are routed to the matching typed provider collection and non-generic providers are skipped without crashing initialization.
        /// </summary>
        [Test]
        public async Task PluginEquipmentProviderManager_InitializesTypedProvidersAndSkipsNonGenericProviders() {
            var genericProvider = new TestDeviceProvider();
            var nonGenericProvider = new NonGenericProvider();
            var pluginLoader = new Mock<IPluginLoader>();
            pluginLoader.Setup(x => x.Load()).Returns(Task.CompletedTask);
            pluginLoader.SetupGet(x => x.DeviceProviders).Returns(new List<IEquipmentProvider> {
                genericProvider,
                nonGenericProvider
            });
            var providers = new PluginEquipmentProviders<ITestDevice>();
            var sut = new PluginEquipmentProviderManager(new IEquipmentProviders[] { providers }, pluginLoader.Object);

            await sut.Initialize();
            IList<IEquipmentProvider<ITestDevice>> registered = await providers.GetProviders();

            providers.Initialized.Should().BeTrue();
            registered.Should().ContainSingle().Which.Should().BeSameAs(genericProvider);
            pluginLoader.Verify(x => x.Load(), Times.Once);
        }

        /// <summary>
        /// Verifies typed plugin equipment providers reject incompatible provider instances and release GetProviders callers once initialized.
        /// </summary>
        [Test]
        public async Task PluginEquipmentProviders_RejectWrongProviderTypeAndWaitUntilInitialized() {
            var providers = new PluginEquipmentProviders<ITestDevice>();
            var pending = providers.GetProviders();

            pending.IsCompleted.Should().BeFalse();
            providers.Initialized = true;
            IList<IEquipmentProvider<ITestDevice>> resolved = await pending.WaitAsync(TimeSpan.FromSeconds(1));
            Action addWrongType = () => providers.AddProvider(new NonGenericProvider());

            resolved.Should().BeEmpty();
            addWrongType.Should().Throw<ArgumentException>();
            providers.GetInterfaceType().Should().Be(typeof(ITestDevice));
        }

        private static Mock<IProfileService> CreateProfileService(IApplicationSettings applicationSettings) {
            var profile = new Mock<IProfile>();
            profile.SetupGet(x => x.ApplicationSettings).Returns(applicationSettings);
            var profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            return profileService;
        }

        private interface ITestBehavior : IPluggableBehavior {
        }

        private sealed class TestBehavior : ITestBehavior {
            public TestBehavior(string name, string contentId) {
                Name = name;
                ContentId = contentId;
            }

            public string Name { get; }
            public string ContentId { get; }
        }

        private sealed class OtherBehavior : IPluggableBehavior {
            public string Name => "Other";
            public string ContentId => "other";
        }

        private interface ITestDevice : IDevice {
        }

        private sealed class TestDevice : ITestDevice {
            public event PropertyChangedEventHandler? PropertyChanged {
                add { }
                remove { }
            }
            public bool HasSetupDialog => false;
            public string Id => "test";
            public string Name => "Test";
            public string DisplayName => "Test";
            public string Category => "Test";
            public bool Connected => false;
            public string Description => "Test";
            public string DriverInfo => "Test";
            public string DriverVersion => "1.0";
            public IList<string> SupportedActions => Array.Empty<string>();
            public Task<bool> Connect(CancellationToken token) {
                return Task.FromResult(true);
            }
            public void Disconnect() {
            }
            public void SetupDialog() {
            }
            public string Action(string actionName, string actionParameters) {
                return string.Empty;
            }
            public string SendCommandString(string command, bool raw = true) {
                return string.Empty;
            }
            public bool SendCommandBool(string command, bool raw = true) {
                return true;
            }
            public void SendCommandBlind(string command, bool raw = true) {
            }
        }

        private sealed class TestDeviceProvider : IEquipmentProvider<ITestDevice> {
            public string Name => "Typed";
            public IList<ITestDevice> GetEquipment() {
                return new List<ITestDevice> {
                    new TestDevice()
                };
            }
        }

        private sealed class NonGenericProvider : IEquipmentProvider {
            public string Name => "Non generic";
        }
    }
}
