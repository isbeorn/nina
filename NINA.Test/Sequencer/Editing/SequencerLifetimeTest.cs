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
using NINA.Sequencer.Editing;
using NINA.Sequencer.Logic;
using Expression = NINA.Sequencer.Logic.Expression;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.Trigger;
using NUnit.Framework;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using static NINA.Test.Sequencer.Editing.CoreEditorTestScope;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SequencerLifetimeTest {
        [TestCase(typeof(NINA.Sequencer.Trigger.Platesolving.CenterAfterDriftTrigger), false)]
        [TestCase(typeof(NINA.Sequencer.Trigger.Platesolving.CenterAfterDriftTrigger), true)]
        [TestCase(typeof(NINA.Sequencer.Trigger.SafetyMonitor.TriggerOnUnsafe), false)]
        [TestCase(typeof(NINA.Sequencer.Trigger.SafetyMonitor.TriggerOnUnsafe), true)]
        [TestCase(typeof(NINA.Sequencer.Trigger.Connect.ReconnectOnDownloadFailure), false)]
        [TestCase(typeof(NINA.Sequencer.Trigger.Connect.ReconnectOnDownloadFailure), true)]
        public void RemovedActiveTriggerAncestor_IsCollected(Type type, bool stillRunning) {
            using var scope = new CoreEditorTestScope();
            var references = InitializeAndRemoveTriggerAncestor(scope, type, stillRunning);
            try {
                AssertCollected(references, scope.ClearServiceInvocations);
            } finally {
                if (references[1].Item2.Target is ISequenceTrigger trigger) {
                    trigger.SequenceBlockTeardown();
                    trigger.SequenceBlockTeardown();
                }
            }
            GC.KeepAlive(scope);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (string, WeakReference)[] InitializeAndRemoveTriggerAncestor(CoreEditorTestScope scope, Type type, bool stillRunning) {
            var ancestor = new SequentialContainer();
            var parent = new SequentialContainer();
            var trigger = (ISequenceTrigger)scope.Create(type);
            ancestor.Add(parent);
            scope.Root.Add(ancestor);
            parent.Status = NINA.Core.Enum.SequenceEntityStatus.RUNNING;
            parent.Add(trigger);
            trigger.SequenceBlockInitialize();
            if (!stillRunning) parent.Status = NINA.Core.Enum.SequenceEntityStatus.FINISHED;
            ancestor.Detach();
            // Repeated ancestor notifications must not reactivate a detached subtree.
            ancestor.AfterParentChanged();
            scope.History.Clear();
            scope.ClearServiceInvocations();
            return new[] { ("removed ancestor", new WeakReference(ancestor)), ("active trigger", new WeakReference(trigger)) };
        }

        public static IEnumerable<TestCaseData> CoreEntities => CoreEditorHistoryTest.CoreEntities
            .Where(type => type != typeof(SequenceRootContainer)) // Roots are replaced, not deleted as child entities.
            .SelectMany(type => new[] { false, true }.Select(view => new TestCaseData(type, view)
                .SetName($"RemovedEntity_IsCollected_{type.Name}_{(view ? "View" : "Model")}")));

        [TestCaseSource(nameof(CoreEntities))]
        public void RemovedEntity_IsCollected(Type type, bool showView) {
            using var scope = new CoreEditorTestScope();
            var references = CreateAndRemove(scope, type, showView);

            AssertCollected(references, scope.ClearServiceInvocations);

            GC.KeepAlive(scope);
        }

        public static IEnumerable<Type> CloneTypes => CoreEditorHistoryTest.CoreEntities.Where(type => type != typeof(SequenceRootContainer));

        [TestCaseSource(nameof(CloneTypes))]
        public void RetainedClone_DoesNotRetainDeletedSource(Type type) {
            using var scope = new CoreEditorTestScope();
            var original = CloneAndRemoveSource(scope, type);
            AssertCollected(new[] { ("clone source", original) });
            GC.KeepAlive(scope);
        }

        [TestCaseSource(nameof(CloneTypes))]
        public void UnloadedEditor_IsCollectedWhileEntityRemainsAttached(Type type) {
            using var scope = new CoreEditorTestScope();
            var references = ShowAndUnloadEditor(scope, type);
            AssertCollected(references);
            GC.KeepAlive(scope);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (string, WeakReference)[] ShowAndUnloadEditor(CoreEditorTestScope scope, Type type) {
            var entity = scope.Create(type);
            scope.Show(entity);
            var view = Descendants<FrameworkElement>(scope.Host).FirstOrDefault(element => element.TemplatedParent is ContentPresenter presenter
                && ReferenceEquals(presenter.Content, entity));
            scope.Host.Content = null;
            scope.Host.DataContext = null;
            scope.Host.ContentTemplate = null;
            Drain();
            scope.ClearServiceInvocations();
            return view == null ? Array.Empty<(string, WeakReference)>() : new[] { ("unloaded editor", new WeakReference(view)) };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference CloneAndRemoveSource(CoreEditorTestScope scope, Type type) {
            var original = scope.Create(type);
            if (original is TimeCondition time) time.SelectedProvider = time.DateTimeProviders.Last();
            Add(scope, original);
            var clone = (ISequenceEntity)original.Clone();
            Add(scope, clone);
            original.DetachCommand.Execute(null);
            scope.History.Clear();
            scope.ClearServiceInvocations();
            return new WeakReference(original);
        }

        private static void Add(CoreEditorTestScope scope, ISequenceEntity entity) {
            switch (entity) {
                case ISequenceCondition condition: scope.Root.Add(condition); break;
                case ISequenceTrigger trigger: scope.Root.Add(trigger); break;
                case ISequenceItem item: scope.Root.Add(item); break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static List<(string Name, WeakReference Reference)> CreateAndRemove(CoreEditorTestScope scope, Type type, bool showView) {
            var references = new List<(string, WeakReference)>();
            var original = scope.Create(type, useRealWatchdog: true);
            if (original is TimeCondition time) time.SelectedProvider = time.DateTimeProviders.Last();
            var clone = (ISequenceEntity)((ICloneable)original).Clone();
            foreach (var entity in new[] { original, clone }) {
                references.Add((ReferenceEquals(entity, original) ? "original" : "clone", new WeakReference(entity)));
                if (showView) {
                    scope.Show(entity);
                    var view = Descendants<FrameworkElement>(scope.Host).FirstOrDefault(element => element.TemplatedParent is ContentPresenter presenter
                        && ReferenceEquals(presenter.Content, entity));
                    // Instructions without settings have no editor template to retain.
                    if (view != null) references.Add(($"view {view.GetType().Name}", new WeakReference(view)));
                } else Add(scope, entity);
                entity.DetachCommand.Execute(null);
                scope.Host.Content = null;
                scope.Host.DataContext = null;
                scope.Host.ContentTemplate = null;
                Drain();
                scope.History.Clear();
            }
            // Invocation recording is a test-only owner of service arguments, unlike the services themselves.
            scope.ClearServiceInvocations();
            return references;
        }

        [TestCase(1)]
        [TestCase(100)]
        public void RepeatedSymbolGraphRemoval_ReleasesConsumersAndStaticCacheEntries(int cycles) {
            using var scope = new CoreEditorTestScope();
            scope.History.IsEnabled = false;
            var global = new GlobalConstant();
            global.Expr = new Expression("10", global);
            scope.Root.Add(global);
            global.Identifier = "gc_audit_global";
            int cachedScopes = UserSymbol.SymbolCache.Count;
            var references = new List<(string, WeakReference)>();
            for (int i = 0; i < cycles; i++) references.AddRange(CreateAndRemoveSymbolGraph(scope, global));

            global.Consumers.Should().BeEmpty();
            AssertCollected(references);
            UserSymbol.SymbolCache.Count.Should().Be(cachedScopes);
            TestContext.Progress.WriteLine($"Symbol graph churn: {cycles} cycles, {references.Count} objects, 0 survivors.");
            GC.KeepAlive(scope);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (string, WeakReference)[] CreateAndRemoveSymbolGraph(CoreEditorTestScope scope, GlobalConstant global) {
            var graph = new SequentialContainer();
            scope.Root.Add(graph);
            var local = new Variable();
            local.Expr = new Expression("2", local);
            local.OriginalExpr = new Expression("2", local);
            graph.Add(local);
            local.Identifier = "gc_audit_local";
            var condition = new LoopCondition();
            graph.Add(condition);
            condition.IterationsExpression = new Expression("gc_audit_global + gc_audit_local", condition) { SymbolBroker = Mock.Of<ISymbolBroker>() };
            condition.IterationsExpression.Evaluate(ignoreRoot: true);
            global.Consumers.Should().ContainKey(condition.IterationsExpression);
            local.Consumers.Should().ContainKey(condition.IterationsExpression);
            graph.DetachCommand.Execute(null);
            scope.ClearServiceInvocations();
            return new[] { ("graph", new WeakReference(graph)), ("local symbol", new WeakReference(local)),
                ("condition", new WeakReference(condition)), ("expression", new WeakReference(condition.IterationsExpression)) };
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClosedTemplateSession_IsCollected(bool delete) {
            using var scope = new CoreEditorTestScope();
            var references = OpenAndCloseTemplateSession(scope, delete);
            AssertCollected(references);
            GC.KeepAlive(scope);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (string, WeakReference)[] OpenAndCloseTemplateSession(CoreEditorTestScope scope, bool delete) {
            var template = new SequentialContainer();
            template.Add(new NINA.Sequencer.SequenceItem.Utility.WaitForTimeSpan());
            var resolver = new TemplateLinkResolver();
            var saved = new TemplatedSequenceContainer((IProfileService)Application.Current.Resources["ProfileService"], "Test", template,
                new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "lifetime.template.json", DisplayName = "Lifetime" }, resolver);
            resolver.UpdateTemplates(new[] { saved }, true, null);
            var linked = new LinkedTemplateContainer(resolver);
            linked.MaterializeFromTemplate(saved, true);
            scope.Root.Add(linked);
            linked.BeginEditTemplateCommand.Execute(null);
            linked.IsEditing.Should().BeTrue();
            var session = scope.History.ForContents(linked);
            session.Should().NotBeSameAs(scope.History);
            var contents = (SequenceContainer)linked.Items.Single();
            contents.DisableEnableCommand.Execute(null);
            session.Position.Should().Be(1);
            if (delete) linked.DetachCommand.Execute(null);
            else linked.CancelEditTemplateCommand.Execute(null);
            scope.History.Clear();
            scope.ClearServiceInvocations();
            return new[] { ("template session", new WeakReference(session)), ("edited contents", new WeakReference(contents)) };
        }

        [TestCase("item")]
        [TestCase("condition")]
        [TestCase("trigger")]
        public void ReplacedRoot_ReleasesAllChildKinds(string childKind) {
            using var scope = new CoreEditorTestScope();
            var removed = ReplaceRoot(scope, childKind);
            try {
                AssertCollected(removed.References);
                GC.KeepAlive(removed.Sequencer);
                GC.KeepAlive(scope);
            } finally {
                // A failing watchdog-lifetime assertion must not leave a background task running.
                CleanupRoot(removed.References[0].Reference);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (NINA.Sequencer.Sequencer Sequencer, (string Name, WeakReference Reference)[] References) ReplaceRoot(CoreEditorTestScope scope, string childKind) {
            var root = new SequenceRootContainer();
            var condition = (MoonAltitudeCondition)scope.Create(typeof(MoonAltitudeCondition), useRealWatchdog: true);
            if (childKind == "condition") root.Add(condition);
            else if (childKind == "item") {
                var container = new SequentialContainer();
                root.Add(container);
                container.Add(condition);
            } else {
                var trigger = (NINA.Sequencer.Trigger.Utility.CustomTrigger)scope.Create(typeof(NINA.Sequencer.Trigger.Utility.CustomTrigger));
                root.Add(trigger);
                trigger.TriggerRunner.Add(condition);
            }
            condition.ConditionWatchdog.WatchdogTask.Should().NotBeNull();
            var sequencer = new NINA.Sequencer.Sequencer(root);
            var watchdog = condition.ConditionWatchdog.WatchdogTask;
            sequencer.MainContainer = root;
            condition.ConditionWatchdog.WatchdogTask.Should().BeSameAs(watchdog, "assigning the same root must preserve its attachment");
            sequencer.MainContainer = new SequenceRootContainer();
            scope.ClearServiceInvocations();
            return (sequencer, new[] { ("old root", new WeakReference(root)), ("watchdog condition", new WeakReference(condition)) });
        }

        [Test]
        public void DiscardedRedoBranch_ReleasesUndoneClone() {
            using var scope = new CoreEditorTestScope();
            var clone = AddCloneAndDiscardRedo(scope);
            AssertCollected(new[] { ("undone clone", clone) });
            GC.KeepAlive(scope);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference AddCloneAndDiscardRedo(CoreEditorTestScope scope) {
            var original = new NINA.Sequencer.SequenceItem.Utility.WaitForTimeSpan();
            scope.Root.Add(original);
            original.AddCloneToParentCommand.Execute(null);
            var clone = scope.Root.Items.Last();
            clone.Should().NotBeSameAs(original);
            scope.History.Undo().Should().BeTrue();
            original.DisableEnableCommand.Execute(null);
            scope.History.CanRedo.Should().BeFalse();
            return new WeakReference(clone);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CleanupRoot(WeakReference reference) {
            if (reference.Target is not SequenceRootContainer root) return;
            foreach (var item in root.GetItemsSnapshot()) item.Detach();
            foreach (var condition in root.GetConditionsSnapshot()) condition.Detach();
            foreach (var trigger in root.GetTriggersSnapshot()) trigger.Detach();
        }

        internal static void AssertCollected(IEnumerable<(string Name, WeakReference Reference)> references, Action? clearInvocations = null) {
            var timeout = Stopwatch.StartNew();
            string[] survivors;
            do {
                Drain();
                clearInvocations?.Invoke();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Drain();
                GC.Collect();
                survivors = references.Where(reference => reference.Reference.IsAlive).Select(reference => reference.Name).ToArray();
                if (survivors.Length == 0) break;
                // Allow cancelled watchdogs and queued WPF cleanup to complete.
                Thread.Sleep(10);
            } while (timeout.Elapsed < TimeSpan.FromSeconds(2));
            survivors.Should().BeEmpty("detached entities and views must be collectible while the editor and services stay alive");
        }
    }
}