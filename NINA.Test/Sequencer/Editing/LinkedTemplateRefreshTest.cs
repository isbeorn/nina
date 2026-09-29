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
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.ViewModel.Sequencer;
using NUnit.Framework;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class LinkedTemplateRefreshTest {
        private NINA.Profile.Profile profile;
        private IProfileService Profiles => Mock.Of<IProfileService>(s => s.ActiveProfile == profile);
        [SetUp]
        public void SetUp() => profile = new NINA.Profile.Profile();
        [TearDown]
        public void TearDown() => profile.Dispose();
        [Test]
        public void QueuedRefreshRechecksExecutionBeforeReplacingChildren() {
            _ = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var resolver = new TemplateLinkResolver();
            var reference = new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Refresh.template.json" };
            resolver.UpdateTemplates(new[] { new TemplatedSequenceContainer(Profiles, "Test", new SequentialContainer(), reference, resolver) }, true, null);
            var linked = new LinkedTemplateContainer(resolver) { TemplateReference = reference, IsExpanded = true };
            var original = linked.Items.Single();
            var root = new SequenceRootContainer();
            root.Add(linked);
            // Construct only the refresh coordinator, without starting the app's equipment/background services.
            var vm = (Sequence2VM)RuntimeHelpers.GetUninitializedObject(typeof(Sequence2VM));
            typeof(Sequence2VM).GetField("sequencer", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(vm, Mock.Of<ISequencer>(s => s.MainContainer == root));
            typeof(Sequence2VM).GetMethod("TemplateLinkResolver_TemplatesChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(vm, new object[] { resolver, EventArgs.Empty });
            vm.IsRunning = true;
            CoreEditorTestScope.Drain();
            linked.Items.Single().Should().BeSameAs(original);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StartupRefreshPreservesOpenEditorSubtree(bool editInner) {
            var resolver = new TemplateLinkResolver();
            var innerReference = new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Inner.template.json" };
            var outerReference = new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Outer.template.json" };
            var innerContent = new SequentialContainer();
            innerContent.Add(new WaitForTimeSpan { Time = 10 });
            var innerTemplate = new TemplatedSequenceContainer(Profiles, "Test", innerContent, innerReference, resolver);
            resolver.UpdateTemplates(new[] { innerTemplate }, true, null);
            var outerContent = new SequentialContainer();
            outerContent.Add(new LinkedTemplateContainer(resolver) { TemplateReference = innerReference, IsExpanded = true });
            resolver.UpdateTemplates(new[] { innerTemplate, new TemplatedSequenceContainer(Profiles, "Test", outerContent, outerReference, resolver) }, true, null);
            var outer = new LinkedTemplateContainer(resolver) { TemplateReference = outerReference, IsExpanded = true };
            var inner = (LinkedTemplateContainer)((ISequenceContainer)outer.Items.Single()).Items.Single();
            (editInner ? inner : outer).BeginEditTemplateCommand.Execute(null);
            var outerOriginal = outer.Items.Single();
            var innerOriginal = inner.Items.Single();
            var vm = (Sequence2VM)RuntimeHelpers.GetUninitializedObject(typeof(Sequence2VM));
            typeof(Sequence2VM).GetMethod("ResolveLinkedTemplates", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(ISequenceContainer), typeof(int), typeof(bool) }, null)!
                .Invoke(vm, new object[] { outer, 0, true });
            outer.Items.Single().Should().BeSameAs(outerOriginal);
            inner.Items.Single().Should().BeSameAs(innerOriginal);
            (editInner ? inner : outer).IsEditing.Should().BeTrue();
        }
    }
}
