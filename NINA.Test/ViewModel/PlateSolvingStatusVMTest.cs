using FluentAssertions;
using NINA.Core.Model;
using NINA.PlateSolving;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class PlateSolvingStatusVMTest {

        /// <summary>
        /// Verifies that concurrent progress reports de-duplicate plate solve results by SolveTime.
        /// This protects the status panel history from duplicate entries when solver callbacks report from different threads.
        /// </summary>
        [Test]
        public async Task ConcurrencyTest() {
            int iterations = 1000;
            PlateSolvingStatusVM status = new PlateSolvingStatusVM();
            List<PlateSolveResult> results = new List<PlateSolveResult>();
            for (int i = 0; i < iterations; i++) {
                results.Add(new PlateSolveResult(new DateTime(2023, 01, 01, 18, 0, 0) + TimeSpan.FromSeconds(i)));
                results.Add(new PlateSolveResult(new DateTime(2023, 01, 01, 18, 0, 0) + TimeSpan.FromSeconds(i)));
            }

            Parallel.For(0, iterations, idx => {
                status.Progress.Report(new PlateSolveProgress { PlateSolveResult = results[idx] });
                status.Progress.Report(new PlateSolveProgress { PlateSolveResult = results[idx + iterations] });
            });

            await Task.Delay(500);

            status.PlateSolveHistory.Count.Should().Be(iterations);
        }

        /// <summary>
        /// Verifies that linked application progress updates the VM status and forwards the same status object to the original progress sink.
        /// This protects callers that need both local plate-solving status display and upstream application status reporting.
        /// </summary>
        [Test]
        public async Task CreateLinkedProgress_ReportsStatusToVmAndOriginalProgressSink() {
            PlateSolvingStatusVM status = new PlateSolvingStatusVM();
            TaskCompletionSource<ApplicationStatus> forwarded = new TaskCompletionSource<ApplicationStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            Progress<ApplicationStatus> original = new Progress<ApplicationStatus>(x => forwarded.SetResult(x));
            ApplicationStatus reported = new ApplicationStatus { Source = "solver", Status = "solving" };

            status.CreateLinkedProgress(original).Report(reported);
            Task completed = await Task.WhenAny(forwarded.Task, Task.Delay(TimeSpan.FromSeconds(1)));

            completed.Should().Be(forwarded.Task);
            status.Status.Should().BeSameAs(reported);
            forwarded.Task.Result.Should().BeSameAs(reported);
        }

        /// <summary>
        /// Verifies that setting a result with an existing SolveTime replaces the existing history slot instead of appending a duplicate.
        /// This protects the invariant that plate-solving history is keyed by solve timestamp.
        /// </summary>
        [Test]
        public void PlateSolveResult_WhenSolveTimeAlreadyExists_ReplacesExistingHistoryEntry() {
            PlateSolvingStatusVM status = new PlateSolvingStatusVM();
            DateTime solveTime = new DateTime(2026, 4, 16, 22, 0, 0);
            PlateSolveResult first = new PlateSolveResult(solveTime);
            PlateSolveResult duplicate = new PlateSolveResult(solveTime);

            status.PlateSolveResult = first;
            status.PlateSolveResult = duplicate;

            status.PlateSolveHistory.Should().ContainSingle();
            status.PlateSolveHistory[0].Should().BeSameAs(first);
            status.PlateSolveResult.Should().BeSameAs(duplicate);
        }
    }
}
