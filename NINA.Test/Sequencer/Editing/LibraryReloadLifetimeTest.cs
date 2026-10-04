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
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using NUnit.Framework;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using static NINA.Test.Sequencer.Editing.CoreEditorTestScope;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class LibraryReloadLifetimeTest {
        [TestCase(false)]
        [TestCase(true)]
        public void ReloadedLibrary_ReleasesPreviousGraphsWhileControllerStaysAlive(bool targets) {
            using var scope = new CoreEditorTestScope();
            using var profile = new NINA.Profile.Profile();
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "library-lifetime-" + Guid.NewGuid().ToString("N"));
            string emptyDirectory = Path.Combine(directory, "empty");
            Directory.CreateDirectory(emptyDirectory);
            profile.SequenceSettings.SequencerTemplatesFolder = directory;
            profile.SequenceSettings.SequencerTargetsFolder = directory;
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            var resolver = new TemplateLinkResolver();
            var factory = new SequencerFactory(profiles.Object,
                new List<ISequenceItem> { (ISequenceItem)scope.Create(typeof(GlobalConstant)), (ISequenceItem)scope.Create(typeof(Variable)) },
                new List<ISequenceCondition>(), new List<ISequenceTrigger>(),
                new List<ISequenceContainer> { (ISequenceContainer)scope.Create(typeof(SequentialContainer)), (ISequenceContainer)scope.Create(typeof(DeepSkyObjectContainer)) },
                new List<IDateTimeProvider>(), new List<ISequenceEntityUpgrader>());
            var converter = new SequenceJsonConverter(factory);
            string identifier = "library_gc_" + Guid.NewGuid().ToString("N");
            WriteLibraryGraph(scope, converter, directory, identifier, targets);
            object controller = targets ? new TargetController(converter, profiles.Object) : new TemplateController(converter, profiles.Object, resolver);
            try {
                PumpUntil(() => GetWatcher(controller)?.EnableRaisingEvents == true && GetField<ISequenceSettings>(controller, "activeSequenceSettings") != null);
                for (int cycle = 0; cycle < 2; cycle++) {
                    Reload(controller, profile.SequenceSettings, directory, 1);
                    var references = CaptureAndReload(controller, profile.SequenceSettings, emptyDirectory);
                    SequencerLifetimeTest.AssertCollected(references, scope.ClearServiceInvocations);
                    UserSymbol.SymbolCache.Values.SelectMany(cache => cache.Values).Should().NotContain(symbol => symbol.Identifier.StartsWith(identifier));
                }
                GC.KeepAlive(controller);
                GC.KeepAlive(resolver);
                GC.KeepAlive(profiles);
                GC.KeepAlive(factory);
            } finally {
                CleanupTestController(controller, profiles.Object);
                // A failed probe must not leave its registrations in other tests.
                foreach (var cache in UserSymbol.SymbolCache.Values.ToList()) {
                    foreach (var symbol in cache.Values.Where(symbol => symbol.Identifier.StartsWith(identifier)).ToList()) symbol.Identifier = "";
                }
                Directory.Delete(directory, true);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteLibraryGraph(CoreEditorTestScope scope, SequenceJsonConverter converter, string directory, string identifier, bool targets) {
            var graph = targets ? (ISequenceContainer)scope.Create(typeof(DeepSkyObjectContainer)) : new SequentialContainer();
            scope.Root.Add(graph);
            foreach (UserSymbol symbol in new UserSymbol[] { new GlobalConstant(), new Variable() }) {
                symbol.Expr = new Expression("1", symbol);
                graph.Add(symbol);
                symbol.Identifier = identifier + symbol.GetType().Name;
            }
            graph.Detach();
            string extension = targets ? TargetController.TargetsFileExtension : TemplateController.TemplateFileExtension;
            File.WriteAllText(Path.Combine(directory, "Lifetime" + extension), converter.Serialize(graph));
            scope.History.Clear();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (string, WeakReference)[] CaptureAndReload(object controller, ISequenceSettings settings, string emptyDirectory) {
            ISequenceContainer graph;
            ISequenceContainer clone;
            if (controller is TargetController targets) {
                graph = targets.Targets.Single().Container;
                clone = targets.Targets.Single().Clone();
            } else {
                var template = ((TemplateController)controller).UserTemplates.Single();
                graph = template.Container;
                clone = (ISequenceContainer)template.Clone();
            }
            graph.GetItemsSnapshot().Should().HaveCount(2).And.OnlyContain(item => item is UserSymbol);
            graph.AfterParentChanged();
            graph.AfterParentChanged();
            foreach (var symbol in graph.GetItemsSnapshot().OfType<UserSymbol>()) {
                UserSymbol.SymbolCache.Values.SelectMany(cache => cache.Values).Should().NotContain(symbol);
            }
            var root = new SequenceRootContainer();
            root.Add(clone);
            foreach (var symbol in clone.GetItemsSnapshot().OfType<UserSymbol>()) {
                UserSymbol.SymbolCache[symbol.SParent()][symbol.Identifier].Should().BeSameAs(symbol);
            }
            clone.Detach();
            var references = graph.GetItemsSnapshot().Select(item => (item.GetType().Name, new WeakReference(item)))
                .Append(("previous library graph", new WeakReference(graph))).ToArray();
            Reload(controller, settings, emptyDirectory, 0);
            return references;
        }

        private static void Reload(object controller, ISequenceSettings settings, string directory, int expectedCount) {
            if (controller is TargetController targets) {
                settings.SequencerTargetsFolder = directory;
                PumpUntil(() => targets.Targets.Count == expectedCount && !targets.TargetsLoading && GetWatcher(controller).EnableRaisingEvents);
            } else {
                var templates = (TemplateController)controller;
                settings.SequencerTemplatesFolder = directory;
                PumpUntil(() => templates.UserTemplates.Count == expectedCount && !templates.TemplatesLoading && GetWatcher(controller).EnableRaisingEvents);
            }
        }

        private static FileSystemWatcher GetWatcher(object controller) => GetField<FileSystemWatcher>(controller,
            controller is TargetController ? "sequenceTargetsFolderWatcher" : "sequenceTemplateFolderWatcher");

        private static T GetField<T>(object instance, string name) =>
            (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

        private static void PumpUntil(Func<bool> completed) {
            var timeout = Stopwatch.StartNew();
            while (!completed()) {
                Drain();
                if (timeout.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Library reload did not finish.");
                Thread.Sleep(10);
            }
        }

        private static void CleanupTestController(object controller, IProfileService profiles) {
            // Controllers live for the app session. Only the test needs to stop its private watcher and subscriptions.
            GetWatcher(controller)?.Dispose();
            var type = controller.GetType();
            profiles.ProfileChanged -= (EventHandler)type.GetMethod("ProfileService_ProfileChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate(typeof(EventHandler), controller);
            string settingsHandler = controller is TargetController ? "SequenceSettings_SequencerTargetsFolderChanged" : "SequenceSettings_SequencerTemplatesFolderChanged";
            profiles.ActiveProfile.SequenceSettings.PropertyChanged -= (PropertyChangedEventHandler)type.GetMethod(settingsHandler, BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate(typeof(PropertyChangedEventHandler), controller);
            if (controller is TargetController targets) {
                targets.TargetsView.Filter = null;
            } else {
                ((TemplateController)controller).TemplatesView.Filter = null;
            }
        }
    }
}