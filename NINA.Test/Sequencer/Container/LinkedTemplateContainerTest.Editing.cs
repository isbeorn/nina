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
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NUnit.Framework;
using NINA.Sequencer.SequenceItem;

namespace NINA.Test.Sequencer.Container {
    public partial class LinkedTemplateContainerTest {
        [Test]
        public async Task Run_ReopenedEditorWaitsForItsOwnCompletion() {
            var (linked, _, probe, clock) = CreateEditingTemplate();
            linked.CancelEditTemplateCommand.Execute(null);
            linked.BeginEditTemplateCommand.Execute(null);
            await using var run = new TestRun(linked, CancellationToken.None, probe);
            await LinkedTemplateTestClock.Until(() => linked.IsWaitingForEdits && clock.PendingTimers > 0);
            probe.Executions.Should().Be(0);
            linked.CancelEditTemplateCommand.Execute(null);
            await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            probe.Release.TrySetResult(true);
            await run.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            probe.Executions.Should().Be(1);
        }

        [Test]
        public async Task Run_StopAndRestartPreservesSessionWithFreshDeadline() {
            var (linked, _, probe, clock) = CreateEditingTemplate();
            var contents = linked.Items.Single();
            using var stop = new CancellationTokenSource();
            await using (var first = new TestRun(linked, stop.Token, probe)) {
                await LinkedTemplateTestClock.Until(() => clock.PendingTimers > 0);
                clock.Advance(TimeSpan.FromSeconds(299));
                stop.Cancel();
                await first.IgnoreCancellation();
            }
            clock.Advance(TimeSpan.FromMinutes(10));
            linked.ResetAll();
            await using var second = new TestRun(linked, CancellationToken.None, probe);
            await LinkedTemplateTestClock.Until(() => linked.IsWaitingForEdits && clock.PendingTimers > 0);
            linked.Items.Single().Should().BeSameAs(contents);
            ((ISequenceContainer)contents).Items.Single().Name.Should().Be("Unsaved");
            linked.LinkStatusText.Should().Contain("05:00");
            probe.Executions.Should().Be(0);
            linked.CancelEditTemplateCommand.Execute(null);
            await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await second.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            probe.Executions.Should().Be(1);
        }

        [Test]
        public async Task Save_RejectsNewDescendantEditorUntilPersistenceFinishes() {
            var (innerSource, resolver, _, _) = CreateEditingTemplate();
            innerSource.CancelEditTemplateCommand.Execute(null);
            resolver.TryResolve(innerSource.TemplateReference, out var innerTemplate);
            var reference = CreateReference("SavingParent.template.json", "Saving parent");
            var finishSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            resolver.UpdateTemplates(new[] { innerTemplate, CreateTemplate(reference, "Outer", innerSource) }, true, async (_, content, _) => {
                await finishSave.Task;
                resolver.UpdateTemplates(new[] { innerTemplate, CreateTemplate(reference, (ISequenceContainer)content.Clone()) }, true, null);
            });
            var outer = new LinkedTemplateContainer(resolver) { TemplateReference = reference, IsExpanded = true };
            outer.BeginEditTemplateCommand.Execute(null);
            var inner = (LinkedTemplateContainer)((ISequenceContainer)outer.Items.Single()).Items.Single();
            var save = outer.SaveTemplateCommand.ExecuteAsync(null);
            try {
                inner.BeginEditTemplateCommand.CanExecute(null).Should().BeFalse();
                inner.BeginEditTemplateCommand.Execute(null);
                inner.IsEditing.Should().BeFalse();
                outer.CanCancelTemplate.Should().BeFalse();
            } finally {
                finishSave.TrySetResult();
                await save.WaitAsync(TimeSpan.FromSeconds(5));
            }
            inner = (LinkedTemplateContainer)((ISequenceContainer)outer.Items.Single()).Items.Single();
            inner.BeginEditTemplateCommand.CanExecute(null).Should().BeTrue();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Run_SaveAtExpiryWinsAndFailedSaveRestartsCountdown(bool fail) {
            var (linked, resolver, probe, clock) = CreateEditingTemplate();
            var saveEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var finishSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            resolver.TryResolve(linked.TemplateReference, out var saved);
            resolver.UpdateTemplates(new[] { saved }, true, async (_, content, _) => {
                saveEntered.SetResult();
                await finishSave.Task;
                if (fail) throw new IOException("Save failed for this regression");
                resolver.UpdateTemplates(new[] { CreateTemplate(linked.TemplateReference, (ISequenceContainer)content.Clone()) }, true, null);
            });
            using var stop = new CancellationTokenSource();
            await using var run = new TestRun(linked, stop.Token, probe);
            await LinkedTemplateTestClock.Until(() => linked.IsWaitingForEdits && clock.PendingTimers > 0);
            clock.Advance(TimeSpan.FromSeconds(299));
            Task save = linked.SaveTemplateCommand.ExecuteAsync(null);
            await saveEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            try {
                linked.CanSaveTemplate.Should().BeFalse();
                linked.CanCancelTemplate.Should().BeFalse();
                linked.CanEditContents.Should().BeFalse();
                linked.CancelEditTemplateCommand.Execute(null);
                clock.Advance(TimeSpan.FromMinutes(10));
                await LinkedTemplateTestClock.Until(() => linked.LinkStatusText.Contains("saving"));
                linked.IsEditing.Should().BeTrue();
                probe.Executions.Should().Be(0);
            } finally { finishSave.TrySetResult(); }
            await save.WaitAsync(TimeSpan.FromSeconds(5));
            if (fail) {
                linked.IsEditing.Should().BeTrue();
                linked.CanEditContents.Should().BeTrue();
                linked.LinkStatusText.Should().Contain("05:00");
                await LinkedTemplateTestClock.Until(() => clock.PendingTimers > 0);
                clock.Advance(TimeSpan.FromSeconds(299));
                await LinkedTemplateTestClock.Until(() => linked.LinkStatusText.Contains("00:01") && clock.PendingTimers > 0);
                probe.Executions.Should().Be(0);
                clock.Advance(TimeSpan.FromSeconds(1));
            }
            var executed = await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            executed.Name.Should().Be(fail ? "Blocking instruction" : "Unsaved");
            probe.Release.TrySetResult(true);
            await run.Completion;
            probe.Executions.Should().Be(1);
        }

        [Test]
        public async Task Run_StopAtExpiryPreservesEditsAndReleasesAdmission() {
            var (linked, _, probe, clock) = CreateEditingTemplate();
            var contents = linked.Items.Single();
            using var stop = new CancellationTokenSource();
            await using var run = new TestRun(linked, stop.Token, probe);
            await LinkedTemplateTestClock.Until(() => linked.IsWaitingForEdits && clock.PendingTimers > 0);
            stop.Cancel();
            clock.Advance(TimeSpan.FromMinutes(5));
            await run.IgnoreCancellation();
            linked.IsEditing.Should().BeTrue();
            linked.Items.Single().Should().BeSameAs(contents);
            ((ISequenceContainer)contents).Items.Single().Name.Should().Be("Unsaved");
            linked.IsWaitingForEdits.Should().BeFalse();
            probe.Executions.Should().Be(0);
            clock.PendingTimers.Should().Be(0);
            linked.CancelEditTemplateCommand.Execute(null);
            linked.BeginEditTemplateCommand.CanExecute(null).Should().BeTrue();
        }

        [Test]
        public async Task Run_TimeoutRejectsLateSave() {
            var (linked, resolver, probe, clock) = CreateEditingTemplate();
            int saves = 0;
            resolver.TryResolve(linked.TemplateReference, out var saved);
            resolver.UpdateTemplates(new[] { saved }, true, (_, _, _) => { saves++; return Task.CompletedTask; });
            await using var run = new TestRun(linked, CancellationToken.None, probe);
            await LinkedTemplateTestClock.Until(() => clock.PendingTimers > 0);
            clock.Advance(TimeSpan.FromMinutes(5));
            await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await linked.SaveTemplateCommand.ExecuteAsync(null);
            saves.Should().Be(0);
            linked.IsEditing.Should().BeFalse();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Run_RestorationFailureNeverExecutesUnsavedContent(bool timeout) {
            var (linked, resolver, probe, clock) = CreateEditingTemplate();
            resolver.TryResolve(linked.TemplateReference, out var saved);
            await using var run = new TestRun(linked, CancellationToken.None, probe);
            await LinkedTemplateTestClock.Until(() => clock.PendingTimers > 0);
            resolver.UpdateTemplates(Array.Empty<TemplatedSequenceContainer>(), true, null);
            if (timeout) clock.Advance(TimeSpan.FromMinutes(5));
            else linked.CancelEditTemplateCommand.Execute(null);
            await run.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            probe.Executions.Should().Be(0);
            linked.Status.Should().Be(SequenceEntityStatus.FAILED);
            linked.IsEditing.Should().BeTrue();
            resolver.UpdateTemplates(new[] { saved }, true, null);
            linked.CancelEditTemplateCommand.Execute(null);
            linked.BeginEditTemplateCommand.CanExecute(null).Should().BeTrue();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Run_NestedEditorsPreservedAndClosedInnermostFirst(bool timeout) {
            var (innerSource, resolver, probe, _) = CreateEditingTemplate();
            innerSource.CancelEditTemplateCommand.Execute(null);
            resolver.TryResolve(innerSource.TemplateReference, out var innerTemplate);
            var outerReference = CreateReference("OuterWait.template.json", "Outer wait");
            resolver.UpdateTemplates(new[] { innerTemplate, CreateTemplate(outerReference, "Outer", innerSource) }, true, null);
            var outer = new LinkedTemplateContainer(resolver) { TemplateReference = outerReference, IsExpanded = true };
            outer.BeginEditTemplateCommand.Execute(null);
            var content = (ISequenceContainer)outer.Items.Single();
            var inner = (LinkedTemplateContainer)content.Items.Single();
            inner.BeginEditTemplateCommand.Execute(null);
            ((ISequenceContainer)inner.Items.Single()).Items.Single().Name = "Inner unsaved";
            outer.TryResolveTemplate();
            outer.Items.Single().Should().BeSameAs(content);
            outer.CanSaveTemplate.Should().BeFalse();
            outer.CanCancelTemplate.Should().BeFalse();
            outer.CancelEditTemplateCommand.Execute(null);
            outer.IsEditing.Should().BeTrue();
            var closed = new List<LinkedTemplateContainer>();
            foreach (var link in new[] { outer, inner }) link.PropertyChanged += (_, e) => {
                if (e.PropertyName == nameof(link.IsEditing) && !link.IsEditing) closed.Add(link);
            };
            var clock = new LinkedTemplateTestClock();
            clock.Install(outer);
            await using var run = new TestRun(outer, CancellationToken.None, probe);
            await LinkedTemplateTestClock.Until(() => clock.PendingTimers > 0);
            clock.Advance(TimeSpan.FromSeconds(299));
            inner.NotifyEditingActivity();
            outer.LinkStatusText.Should().Contain("05:00");
            if (timeout) clock.Advance(TimeSpan.FromMinutes(5));
            else {
                inner.CancelEditTemplateCommand.Execute(null);
                outer.CanCancelTemplate.Should().BeTrue();
                outer.CanSaveTemplate.Should().BeTrue();
                outer.CancelEditTemplateCommand.Execute(null);
            }
            var executed = await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            executed.Name.Should().Be("Blocking instruction");
            closed.Should().Equal(inner, outer);
            probe.Release.TrySetResult(true);
            await run.Completion;
            probe.Executions.Should().Be(1);
        }

        [Test]
        public async Task Run_WaitingEditorDoesNotBlockParallelBranch() {
            var (linked, _, probe, clock) = CreateEditingTemplate();
            var independent = new BlockingProbe();
            var parallel = new ParallelContainer();
            parallel.Add(linked);
            parallel.Add(new BlockingInstruction(independent));
            await using var run = new TestRun(parallel, CancellationToken.None, probe, independent);
            await independent.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await LinkedTemplateTestClock.Until(() => linked.IsWaitingForEdits && clock.PendingTimers > 0);
            independent.Release.TrySetResult(true);
            probe.Executions.Should().Be(0);
            linked.CancelEditTemplateCommand.Execute(null);
            await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            probe.Release.TrySetResult(true);
            await run.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            independent.Executions.Should().Be(1);
            probe.Executions.Should().Be(1);
        }

        [Test]
        public async Task Run_ConcurrentEditAndStartAdmitExactlyOneOwner() {
            for (int attempt = 0; attempt < 20; attempt++) {
                var (linked, _, probe, _) = CreateEditingTemplate();
                linked.CancelEditTemplateCommand.Execute(null);
                using var stop = new CancellationTokenSource();
                var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var edit = Task.Run(async () => { await ready.Task; linked.BeginEditTemplateCommand.Execute(null); });
                var execution = Task.Run(async () => { await ready.Task; await linked.Run(new Progress<ApplicationStatus>(), stop.Token); });
                ready.SetResult();
                try {
                    await edit.WaitAsync(TimeSpan.FromSeconds(5));
                    await LinkedTemplateTestClock.Until(() => linked.IsWaitingForEdits || probe.Started.Task.IsCompleted);
                    if (linked.IsEditing) {
                        probe.Executions.Should().Be(0);
                        linked.CancelEditTemplateCommand.Execute(null);
                    } else {
                        linked.BeginEditTemplateCommand.CanExecute(null).Should().BeFalse();
                    }
                    await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    linked.IsEditing.Should().BeFalse();
                    probe.Release.TrySetResult(true);
                    await execution.WaitAsync(TimeSpan.FromSeconds(5));
                    probe.Executions.Should().Be(1);
                    linked.BeginEditTemplateCommand.CanExecute(null).Should().BeTrue();
                } finally {
                    stop.Cancel();
                    probe.Release.TrySetResult(true);
                    try { await execution.WaitAsync(TimeSpan.FromSeconds(5)); } catch (OperationCanceledException) { }
                }
            }
        }

        private (LinkedTemplateContainer, TemplateLinkResolver, BlockingProbe, LinkedTemplateTestClock) CreateEditingTemplate() {
            var reference = CreateReference("Editing.template.json", "Editing");
            var resolver = new TemplateLinkResolver();
            var probe = new BlockingProbe();
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "Editing", new BlockingInstruction(probe)) }, true, null);
            var linked = new LinkedTemplateContainer(resolver) { TemplateReference = reference, IsExpanded = true };
            var clock = new LinkedTemplateTestClock();
            clock.Install(linked);
            linked.BeginEditTemplateCommand.Execute(null);
            ((ISequenceContainer)linked.Items.Single()).Items.Single().Name = "Unsaved";
            return (linked, resolver, probe, clock);
        }

        private sealed class TestRun : IAsyncDisposable {
            private readonly CancellationTokenSource stop;
            private readonly BlockingProbe[] probes;
            public Task Completion { get; }
            public TestRun(ISequenceItem item, CancellationToken token, params BlockingProbe[] probes) {
                stop = CancellationTokenSource.CreateLinkedTokenSource(token);
                this.probes = probes;
                Completion = item.Run(new Progress<ApplicationStatus>(), stop.Token);
            }
            public async Task IgnoreCancellation() {
                try { await Completion.WaitAsync(TimeSpan.FromSeconds(5)); } catch (OperationCanceledException) { }
            }
            public async ValueTask DisposeAsync() {
                stop.Cancel();
                foreach (var probe in probes) probe.Release.TrySetResult(true);
                await IgnoreCancellation();
                stop.Dispose();
            }
        }

        [Test]
        public async Task Run_EditTimeoutRestoresSavedTemplateAndRunsOnce() {
            var reference = CreateReference("Timeout.template.json", "Timeout");
            var resolver = new TemplateLinkResolver();
            var probe = new BlockingProbe();
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "Timeout", new BlockingInstruction(probe)) }, true, null);
            var linked = new LinkedTemplateContainer(resolver) { TemplateReference = reference };
            var clock = new LinkedTemplateTestClock();
            clock.Install(linked);
            linked.TryResolveTemplate();
            linked.BeginEditTemplateCommand.Execute(null);
            ((ISequenceContainer)linked.Items.Single()).Items.Single().Name = "Unsaved";
            using var cancellation = new CancellationTokenSource();
            Task run = linked.Run(Mock.Of<IProgress<ApplicationStatus>>(), cancellation.Token);
            try {
                await LinkedTemplateTestClock.Until(() => linked.IsWaitingForEdits && clock.PendingTimers > 0);
                clock.Advance(TimeSpan.FromMinutes(5));
                var executing = await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
                executing.Name.Should().Be("Blocking instruction");
                linked.IsEditing.Should().BeFalse();
                probe.Release.TrySetResult(true);
                await run.WaitAsync(TimeSpan.FromSeconds(5));
                probe.Executions.Should().Be(1);
            } finally {
                cancellation.Cancel();
                probe.Release.TrySetResult(true);
                try { await run.WaitAsync(TimeSpan.FromSeconds(5)); } catch (OperationCanceledException) { }
            }
        }
    }
}
