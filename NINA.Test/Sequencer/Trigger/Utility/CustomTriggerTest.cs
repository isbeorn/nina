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
using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Utility;
using NINA.Sequencer.Validations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using SequenceItemBase = NINA.Sequencer.SequenceItem.SequenceItem;

namespace NINA.Test.Sequencer.Trigger.Utility {

    [TestFixture]
    public class CustomTriggerTest {
        private Mock<IApplicationResourceDictionary> resourceDictionaryMock;
        private CustomTrigger sut;

        [SetUp]
        public void Setup() {
            resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
            resourceDictionaryMock.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());

            sut = new CustomTrigger(resourceDictionaryMock.Object) {
                Name = "Custom Trigger",
                Description = "Description",
                Category = "Utility",
                Icon = new GeometryGroup()
            };
        }

        /// <summary>
        /// Verifies cloning preserves trigger metadata and keeps nested trigger/action areas independent.
        /// </summary>
        [Test]
        public void Clone_CopiesMetadataAndNestedAreas() {
            TestTrigger source = new TestTrigger();
            TestInstruction instruction = new TestInstruction();
            sut.TriggerSource = source;
            sut.TriggerRunner.Add(instruction);

            CustomTrigger clone = (CustomTrigger)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Name.Should().Be(sut.Name);
            clone.Description.Should().Be(sut.Description);
            clone.Category.Should().Be(sut.Category);
            clone.Icon.Should().BeSameAs(sut.Icon);
            clone.TriggerSource.Should().NotBeSameAs(source);
            clone.TriggerRunner.Should().NotBeSameAs(sut.TriggerRunner);
            clone.TriggerRunner.GetItemsSnapshot().Should().ContainSingle()
                .Which.Should().NotBeSameAs(instruction);
        }

        /// <summary>
        /// Verifies dropping a trigger into the source area replaces the previous source trigger.
        /// </summary>
        [Test]
        public void DropIntoTriggerSource_ReplacesExistingSourceTrigger() {
            TestTrigger first = new TestTrigger();
            TestTrigger second = new TestTrigger();

            sut.DropIntoTriggerSourceCommand.Execute(new DropIntoParameters(first));
            sut.DropIntoTriggerSourceCommand.Execute(new DropIntoParameters(second));

            sut.TriggerSource.Should().BeOfType<TestTrigger>();
            sut.TriggerSource.Should().NotBeSameAs(first);
            sut.TriggerSource.Should().NotBeSameAs(second);
            sut.TriggerSource.Parent.Should().NotBeNull();
            first.Parent.Should().BeNull();
            second.Parent.Should().BeNull();
        }

        /// <summary>
        /// Verifies the direct trigger source property rejects self-references.
        /// </summary>
        [Test]
        public void TriggerSource_DoesNotAcceptSelf() {
            sut.TriggerSource = sut;

            sut.TriggerSource.Should().BeNull();
            sut.Parent.Should().BeNull();
        }

        /// <summary>
        /// Verifies dropping the custom trigger onto its own source area is ignored.
        /// </summary>
        [Test]
        public void DropIntoTriggerSource_IgnoresSelf() {
            sut.DropIntoTriggerSourceCommand.Execute(new DropIntoParameters(sut));

            sut.TriggerSource.Should().BeNull();
            sut.Parent.Should().BeNull();
        }

        /// <summary>
        /// Verifies the custom trigger uses the nested trigger only as the trigger predicate.
        /// </summary>
        [Test]
        public void ShouldTrigger_DelegatesToNestedSourceTrigger() {
            TestTrigger source = new TestTrigger() {
                ShouldTriggerResult = true,
                ShouldTriggerAfterResult = true
            };
            sut.TriggerSource = source;

            sut.ShouldTrigger(null, null).Should().BeTrue();
            sut.ShouldTriggerAfter(null, null).Should().BeTrue();

            source.ShouldTriggerCount.Should().Be(1);
            source.ShouldTriggerAfterCount.Should().Be(1);
        }

        /// <summary>
        /// Verifies a disabled nested trigger does not fire the custom trigger.
        /// </summary>
        [Test]
        public void ShouldTrigger_DisabledNestedTrigger_ReturnsFalse() {
            TestTrigger source = new TestTrigger() {
                Status = SequenceEntityStatus.DISABLED,
                ShouldTriggerResult = true,
                ShouldTriggerAfterResult = true
            };
            sut.TriggerSource = source;

            sut.ShouldTrigger(null, null).Should().BeFalse();
            sut.ShouldTriggerAfter(null, null).Should().BeFalse();
        }

        /// <summary>
        /// Verifies execution runs the custom instructions and does not run the nested trigger action.
        /// </summary>
        [Test]
        public async Task Execute_RunsCustomInstructionsOnly() {
            TestTrigger source = new TestTrigger();
            TestInstruction instruction = new TestInstruction();
            sut.TriggerSource = source;
            sut.TriggerRunner.Add(instruction);

            await sut.Execute(new SequentialContainer(), Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            instruction.ExecuteCount.Should().Be(1);
            source.ExecuteCount.Should().Be(0);
        }

        /// <summary>
        /// Verifies execution resets stale trigger-runner progress before running custom instructions.
        /// </summary>
        [Test]
        public async Task Execute_ResetsTriggerRunnerBeforeRun() {
            TestInstruction instruction = new TestInstruction() {
                Status = SequenceEntityStatus.FINISHED
            };
            sut.TriggerRunner.Add(instruction);

            await sut.Execute(new SequentialContainer(), Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            instruction.ExecuteCount.Should().Be(1);
            instruction.Status.Should().Be(SequenceEntityStatus.FINISHED);
        }

        /// <summary>
        /// Verifies custom trigger instructions are registered with the real sequence root so the global skip command can skip them.
        /// </summary>
        [Test]
        public async Task Execute_RegistersRunningInstructionWithRootForSkip() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer context = new SequentialContainer();
            BlockingInstruction instruction = new BlockingInstruction();
            root.Add(context);
            sut.AttachNewParent(context);
            sut.TriggerRunner.Add(instruction);

            Task executeTask = sut.Execute(context, Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            await instruction.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            root.GetCurrentRunningItems().Should().ContainSingle().Which.Should().BeSameAs(instruction);

            root.SkipCurrentRunningItems();
            await executeTask.WaitAsync(TimeSpan.FromSeconds(5));

            instruction.Status.Should().Be(SequenceEntityStatus.SKIPPED);
            root.GetCurrentRunningItems().Should().BeEmpty();
        }

        /// <summary>
        /// Verifies the trigger runner does not walk back into the owning trigger set and evaluate its own trigger source while executing.
        /// </summary>
        [Test]
        public async Task Execute_DoesNotEvaluateOwningTriggerSourceFromParentTriggerSet() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer context = new SequentialContainer();
            TestTrigger source = new TestTrigger() {
                ShouldTriggerResult = false,
                ShouldTriggerAfterResult = false
            };
            TestInstruction instruction = new TestInstruction();

            root.Add(context);
            sut.TriggerSource = source;
            sut.TriggerRunner.Add(instruction);
            context.Add(sut);

            await sut.Execute(context, Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            instruction.ExecuteCount.Should().Be(1);
            source.ShouldTriggerCount.Should().Be(0);
            source.ShouldTriggerAfterCount.Should().Be(0);
        }

        /// <summary>
        /// Verifies validation reports missing configuration and passes once a trigger source and instruction exist.
        /// </summary>
        [Test]
        public void Validate_RequiresTriggerSourceAndInstruction() {
            sut.Validate().Should().BeFalse();
            sut.Issues.Should().HaveCount(2);

            sut.TriggerSource = new TestTrigger();
            sut.TriggerRunner.Add(new TestInstruction());

            sut.Validate().Should().BeTrue();
            sut.Issues.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies trigger runner issues are shown without blocking the custom trigger itself.
        /// </summary>
        [Test]
        public void Validate_ReportsTriggerRunnerIssuesWithoutBlockingTrigger() {
            sut.TriggerSource = new TestTrigger();
            sut.TriggerRunner.Add(new TestInstruction());
            sut.TriggerRunner.Issues.Add("runner issue");

            sut.Validate().Should().BeTrue();

            sut.Issues.Should().ContainSingle().Which.Should().Be("runner issue");
        }

        /// <summary>
        /// Verifies lifecycle calls are forwarded to the nested source trigger.
        /// </summary>
        [Test]
        public void Lifecycle_ForwardsToSourceTrigger() {
            TestTrigger source = new TestTrigger();
            sut.TriggerSource = source;

            sut.Initialize();
            sut.SequenceBlockInitialize();
            sut.SequenceBlockStarted();
            sut.SequenceBlockFinished();
            sut.SequenceBlockTeardown();
            sut.Teardown();

            source.InitializeCount.Should().Be(1);
            source.SequenceBlockInitializeCount.Should().Be(1);
            source.SequenceBlockStartedCount.Should().Be(1);
            source.SequenceBlockFinishedCount.Should().Be(1);
            source.SequenceBlockTeardownCount.Should().Be(1);
            source.TeardownCount.Should().Be(1);
        }

        /// <summary>
        /// Verifies the nested trigger's existing detach command clears the direct trigger source.
        /// </summary>
        [Test]
        public void DetachSourceTrigger_ClearsTriggerSource() {
            TestTrigger source = new TestTrigger();
            sut.TriggerSource = source;

            source.Detach();

            sut.TriggerSource.Should().BeNull();
            source.Parent.Should().BeNull();
        }

        /// <summary>
        /// Verifies custom trigger templates do not persist the runtime-only source parent container.
        /// </summary>
        [Test]
        public void SequenceJsonConverter_RoundTripsWithoutSerializingTriggerSourceRuntimeParent() {
            ISequencerFactory factory = CreateSerializationFactory();
            SequenceJsonConverter converter = new SequenceJsonConverter(factory);
            SequentialContainer container = new SequentialContainer() {
                Name = "Template"
            };
            sut.TriggerSource = new TestTrigger();
            sut.TriggerRunner.Add(new TestInstruction());
            container.Add(sut);

            string json = converter.Serialize(container);

            json.Should().Contain(nameof(CustomTrigger));
            json.Should().Contain(nameof(TestTrigger));
            json.Should().NotContain("TriggerSourceParent");

            ISequenceContainer result = converter.Deserialize(json, @"C:\Templates\Custom.template.json");
            ISequenceTrigger roundTrippedTrigger = ((ITriggerable)result).GetTriggersSnapshot().Should().ContainSingle().Which;
            CustomTrigger roundTripped = roundTrippedTrigger.Should().BeOfType<CustomTrigger>().Subject;
            roundTripped.TriggerSource.Should().BeOfType<TestTrigger>();
            roundTripped.TriggerSource.Parent.Should().NotBeNull();
            roundTripped.TriggerSource.Parent.GetType().Name.Should().Be("TriggerSourceParent");
            roundTripped.TriggerRunner.GetItemsSnapshot().Should().ContainSingle()
                .Which.Should().BeOfType<TestInstruction>();
        }

        private ISequencerFactory CreateSerializationFactory() {
            Mock<ISequencerFactory> factoryMock = new Mock<ISequencerFactory>();
            factoryMock.SetupGet(x => x.Upgraders).Returns(new List<ISequenceEntityUpgrader>());
            factoryMock.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => new SequentialContainer());
            factoryMock.Setup(x => x.GetItem<TestInstruction>()).Returns(() => new TestInstruction());
            factoryMock.Setup(x => x.GetTrigger<CustomTrigger>()).Returns(() => new CustomTrigger(resourceDictionaryMock.Object));
            factoryMock.Setup(x => x.GetTrigger<TestTrigger>()).Returns(() => new TestTrigger());
            return factoryMock.Object;
        }

        private sealed class TestTrigger : SequenceTrigger, IValidatable {
            public bool ShouldTriggerResult { get; set; }
            public bool ShouldTriggerAfterResult { get; set; }
            public int ShouldTriggerCount { get; private set; }
            public int ShouldTriggerAfterCount { get; private set; }
            public int ExecuteCount { get; private set; }
            public int InitializeCount { get; private set; }
            public int SequenceBlockInitializeCount { get; private set; }
            public int SequenceBlockStartedCount { get; private set; }
            public int SequenceBlockFinishedCount { get; private set; }
            public int SequenceBlockTeardownCount { get; private set; }
            public int TeardownCount { get; private set; }
            public IList<string> Issues { get; set; } = new List<string>();

            public override object Clone() {
                return new TestTrigger() {
                    ShouldTriggerResult = ShouldTriggerResult,
                    ShouldTriggerAfterResult = ShouldTriggerAfterResult
                };
            }

            public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
                ShouldTriggerCount++;
                return ShouldTriggerResult;
            }

            public override bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem) {
                ShouldTriggerAfterCount++;
                return ShouldTriggerAfterResult;
            }

            public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
                ExecuteCount++;
                return Task.CompletedTask;
            }

            public override void Initialize() {
                InitializeCount++;
            }

            public override void SequenceBlockInitialize() {
                SequenceBlockInitializeCount++;
            }

            public override void SequenceBlockStarted() {
                SequenceBlockStartedCount++;
            }

            public override void SequenceBlockFinished() {
                SequenceBlockFinishedCount++;
            }

            public override void SequenceBlockTeardown() {
                SequenceBlockTeardownCount++;
            }

            public override void Teardown() {
                TeardownCount++;
            }

            public bool Validate() {
                return Issues.Count == 0;
            }
        }

        private sealed class TestInstruction : SequenceItemBase {
            public int ExecuteCount { get; private set; }

            public override object Clone() {
                return new TestInstruction();
            }

            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
                ExecuteCount++;
                return Task.CompletedTask;
            }
        }

        private sealed class BlockingInstruction : SequenceItemBase {
            public TaskCompletionSource<bool> Started { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public override object Clone() {
                return new BlockingInstruction();
            }

            public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
                Started.TrySetResult(true);
                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }
        }
    }
}
