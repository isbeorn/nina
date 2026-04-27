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
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace NINA.Test.Sequencer {

    [TestFixture]
    [NonParallelizable]
    [Apartment(ApartmentState.STA)]
    public class TemplateControllerTest {
        private NINA.Profile.Profile profile;
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void SetUp() {
            if (Application.Current == null) {
                _ = new Application {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }

            profile = new NINA.Profile.Profile();
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        [Test]
        public async Task SaveLinkedTemplate_ConcurrentSavesDoNotDuplicateUserTemplate() {
            string templatePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(templatePath);
            profile.SequenceSettings.SequencerTemplatesFolder = templatePath;
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            SequenceJsonConverter converter = CreateSequenceJsonConverter(resolver);
            TemplateController sut = new TemplateController(converter, profileServiceMock.Object, resolver);
            await WaitWithDispatcher(resolver.WaitForInitialLoad(CancellationToken.None));
            TemplateReference reference = new TemplateReference {
                SourceKind = TemplateReferenceSourceKind.User,
                RelativePath = "Edited.template.json",
                DisplayName = "Edited"
            };
            SequentialContainer template = new SequentialContainer {
                Name = "Edited"
            };

            Task firstSave = resolver.SaveTemplate(reference, template, CancellationToken.None);
            Task secondSave = resolver.SaveTemplate(reference, template, CancellationToken.None);
            await WaitWithDispatcher(Task.WhenAll(firstSave, secondSave));
            DrainDispatcher();

            sut.UserTemplates.Where(t =>
                t.Reference != null
                && t.Reference.SourceKind == TemplateReferenceSourceKind.User
                && t.Reference.RelativePath == "Edited.template.json").Should().ContainSingle();
        }

        private SequenceJsonConverter CreateSequenceJsonConverter(TemplateLinkResolver resolver) {
            SequencerFactory factory = new SequencerFactory(
                profileServiceMock.Object,
                new List<ISequenceItem>(),
                new List<ISequenceCondition>(),
                new List<ISequenceTrigger>(),
                new List<ISequenceContainer> { new SequentialContainer(), new LinkedTemplateContainer(resolver) },
                new List<IDateTimeProvider>(),
                new List<ISequenceEntityUpgrader>());
            return new SequenceJsonConverter(factory);
        }

        private static async Task WaitWithDispatcher(Task task) {
            DateTime timeout = DateTime.UtcNow.AddSeconds(10);
            while (!task.IsCompleted) {
                DrainDispatcher();
                if (DateTime.UtcNow > timeout) {
                    throw new TimeoutException("Timed out while waiting for template controller work to complete.");
                }

                await Task.Delay(10);
            }

            await task;
        }

        private static void DrainDispatcher() {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }
}
