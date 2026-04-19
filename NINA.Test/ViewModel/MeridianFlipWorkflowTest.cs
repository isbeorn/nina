using FluentAssertions;
using NINA.WPF.Base.ViewModel;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class MeridianFlipWorkflowTest {

        /// <summary>
        /// Verifies that AutomatedWorkflow executes steps in insertion order, preserves the last active step,
        /// and keeps processing after a step returns false while aggregating the final success result.
        /// This protects meridian-flip orchestration from skipping cleanup or resume steps after a non-critical failure.
        /// </summary>
        [Test]
        public async Task Process_ExecutesAllStepsInOrderAndAggregatesSuccess() {
            AutomatedWorkflow workflow = new AutomatedWorkflow();
            List<string> calls = new List<string>();
            WorkflowStep first = new WorkflowStep("first", "First", () => {
                calls.Add("first");
                return Task.FromResult(true);
            });
            WorkflowStep second = new WorkflowStep("second", "Second", () => {
                calls.Add("second");
                return Task.FromResult(false);
            });
            WorkflowStep third = new WorkflowStep("third", "Third", () => {
                calls.Add("third");
                return Task.FromResult(true);
            });
            workflow.Add(first);
            workflow.Add(second);
            workflow.Add(third);

            bool success = await workflow.Process();

            success.Should().BeFalse();
            calls.Should().Equal("first", "second", "third");
            first.Finished.Should().BeTrue();
            second.Finished.Should().BeFalse();
            third.Finished.Should().BeTrue();
            workflow.ActiveStep.Should().BeSameAs(third);
        }

        /// <summary>
        /// Verifies that AutomatedWorkflow implements collection semantics for containment, copying, removal, and clearing.
        /// This protects UI and workflow code that treats meridian-flip steps as a mutable ordered collection.
        /// </summary>
        [Test]
        public void CollectionMembers_ReflectWorkflowStepMembership() {
            AutomatedWorkflow workflow = new AutomatedWorkflow();
            WorkflowStep first = new WorkflowStep("first", "First", () => Task.FromResult(true));
            WorkflowStep second = new WorkflowStep("second", "Second", () => Task.FromResult(true));
            workflow.Add(first);
            workflow.Add(second);
            WorkflowStep[] copied = new WorkflowStep[2];

            workflow.Contains(first).Should().BeTrue();
            workflow.Count.Should().Be(2);
            workflow.CopyTo(copied, 0);
            copied.Should().Equal(first, second);
            workflow.Remove(first).Should().BeTrue();
            workflow.Should().Equal(second);

            workflow.Clear();

            workflow.Count.Should().Be(0);
        }

        /// <summary>
        /// Verifies that WorkflowStep.Process stores the action result in Finished and returns the same value to the caller.
        /// This protects the status display that marks each meridian-flip step as completed only when its action succeeds.
        /// </summary>
        [Test]
        public async Task WorkflowStepProcess_StoresAndReturnsActionResult() {
            WorkflowStep step = new WorkflowStep("settle", "Settle", () => Task.FromResult(true));

            bool success = await step.Process();

            success.Should().BeTrue();
            step.Finished.Should().BeTrue();
        }
    }
}
