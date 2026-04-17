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
using NINA.Equipment.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NINA.View.Equipment;
using NINA.View.Equipment.Guider;
using NINA.View.Equipment.Switch;
using NINA.ViewModel.Plugins;
using NINA.WPF.Base.Utility;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class NinaTemplateSelectorBehaviorTest {

        /// <summary>
        /// Verifies plugin state selectors map every install/update state to the intended template and default safely for invalid data.
        /// </summary>
        [Test]
        public void PluginStateTemplateSelector_MapsAllPluginStates() {
            var installed = new DataTemplate();
            var notInstalled = new DataTemplate();
            var update = new DataTemplate();
            var restart = new DataTemplate();
            var selector = new PluginStateToDataTemplateSelector {
                Installed = installed,
                NotInstalled = notInstalled,
                UpdateAvailable = update,
                InstalledAndRequiresRestart = restart
            };

            selector.SelectTemplate(PluginState.Installed, null).Should().BeSameAs(installed);
            selector.SelectTemplate(PluginState.NotInstalled, null).Should().BeSameAs(notInstalled);
            selector.SelectTemplate(PluginState.UpdateAvailable, null).Should().BeSameAs(update);
            selector.SelectTemplate(PluginState.InstalledAndRequiresRestart, null).Should().BeSameAs(restart);
            selector.SelectTemplate("bad data", null).Should().BeSameAs(notInstalled);
        }

        /// <summary>
        /// Verifies plugin installer selectors preserve the installer-type contract used by available-plugin descriptions.
        /// </summary>
        [Test]
        public void PluginInstallerDescriptionTemplateSelector_MapsInstallerTypesWithSetupFallback() {
            var dll = new DataTemplate();
            var archive = new DataTemplate();
            var setup = new DataTemplate();
            var selector = new PluginInstallerDescriptionTemplateSelector {
                DLL = dll,
                Archive = archive,
                Setup = setup
            };

            selector.SelectTemplate(InstallerType.DLL, null).Should().BeSameAs(dll);
            selector.SelectTemplate(InstallerType.ARCHIVE, null).Should().BeSameAs(archive);
            selector.SelectTemplate(null, null).Should().BeSameAs(setup);
        }

        /// <summary>
        /// Verifies plugin option templates are discovered by the documented plugin-name plus options-postfix key and fall back when missing.
        /// </summary>
        [Test]
        public void PluginOptionsTemplateSelector_UsesPluginResourceKeyOrDefault() {
            EnsureApplication();
            var pluginTemplate = new DataTemplate();
            var defaultTemplate = new DataTemplate();
            string key = "Test Plugin" + DataTemplatePostfix.Options;
            Application.Current.Resources.Remove(key);
            Application.Current.Resources[key] = pluginTemplate;
            var selector = new PluginOptionsDataTemplateSelector {
                Default = defaultTemplate
            };

            selector.SelectTemplate(new PluginManifest { Name = "Test Plugin" }, null).Should().BeSameAs(pluginTemplate);
            selector.SelectTemplate(new PluginManifest { Name = "Missing Plugin" }, null).Should().BeSameAs(defaultTemplate);
            selector.SelectTemplate(new object(), null).Should().BeSameAs(defaultTemplate);
        }

        /// <summary>
        /// Verifies switch value templates distinguish boolean writable switches, range writable switches, and read-only switches.
        /// </summary>
        [Test]
        public void SwitchTemplateSelector_DistinguishesWritableBooleanRangeAndReadOnlySwitches() {
            var booleanTemplate = new DataTemplate();
            var writableTemplate = new DataTemplate();
            var readOnlyTemplate = new DataTemplate();
            var selector = new SwitchTemplateSelector {
                WritableBoolean = booleanTemplate,
                Writable = writableTemplate,
                ReadOnly = readOnlyTemplate
            };

            selector.SelectTemplate(new TestWritableSwitch(0, 1, 1), null).Should().BeSameAs(booleanTemplate);
            selector.SelectTemplate(new TestWritableSwitch(0, 10, 0.5), null).Should().BeSameAs(writableTemplate);
            selector.SelectTemplate(new TestReadOnlySwitch(), null).Should().BeSameAs(readOnlyTemplate);
        }

        /// <summary>
        /// Verifies resource-backed template selectors return explicit templates, failed-load templates, and defaults deterministically.
        /// </summary>
        [Test]
        public void ResourceBackedTemplateSelectors_UseTemplateKeysAndFailedLoadFallbacks() {
            EnsureApplication();
            var item = new ResourceBackedTemplateItem();
            string postfix = "_Details";
            string key = typeof(ResourceBackedTemplateItem).FullName + postfix;
            Application.Current.Resources.Remove(key);

            var expected = new DataTemplate();
            var defaultTemplate = new DataTemplate();
            var failedTemplate = new DataTemplate();
            var cameraSelector = new CameraTemplateSelector {
                Default = defaultTemplate,
                FailedToLoadTemplate = failedTemplate,
                Postfix = postfix
            };
            var guiderSelector = new GuiderTemplateSelector {
                Default = defaultTemplate,
                FailedToLoadTemplate = failedTemplate,
                Postfix = postfix
            };
            var switchHubSelector = new SwitchHubTemplateSelector {
                Generic = defaultTemplate,
                FailedToLoadTemplate = failedTemplate,
                Postfix = postfix
            };

            cameraSelector.SelectTemplate(item, null).Should().BeSameAs(defaultTemplate);
            guiderSelector.SelectTemplate(item, null).Should().BeSameAs(defaultTemplate);
            switchHubSelector.SelectTemplate(item, null).Should().BeSameAs(defaultTemplate);

            Application.Current.Resources[key] = expected;
            cameraSelector.SelectTemplate(item, null).Should().BeSameAs(expected);
            guiderSelector.SelectTemplate(item, null).Should().BeSameAs(expected);
            switchHubSelector.SelectTemplate(item, null).Should().BeSameAs(expected);

            Application.Current.Resources[key] = "not a template";
            cameraSelector.SelectTemplate(item, null).Should().BeSameAs(failedTemplate);
            guiderSelector.SelectTemplate(item, null).Should().BeSameAs(failedTemplate);
            switchHubSelector.SelectTemplate(item, null).Should().BeSameAs(failedTemplate);
        }

        private static void EnsureApplication() {
            if (Application.Current == null) {
                _ = new Application {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }
        }

        private sealed class ResourceBackedTemplateItem {
        }

        private sealed class TestReadOnlySwitch : ISwitch {
            public short Id => 1;
            public string Name => "Read only";
            public string Description => "Read-only switch";
            public double Value => 0;
            public bool Poll() {
                return true;
            }
        }

        private sealed class TestWritableSwitch : IWritableSwitch {
            public TestWritableSwitch(double minimum, double maximum, double stepSize) {
                Minimum = minimum;
                Maximum = maximum;
                StepSize = stepSize;
            }

            public short Id => 2;
            public string Name => "Writable";
            public string Description => "Writable switch";
            public double Value => TargetValue;
            public double Maximum { get; }
            public double Minimum { get; }
            public double StepSize { get; }
            public double TargetValue { get; set; }
            public bool Poll() {
                return true;
            }
            public void SetValue() {
            }
        }
    }
}
