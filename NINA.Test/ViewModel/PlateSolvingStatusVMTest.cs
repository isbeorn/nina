using FluentAssertions;
using NINA.PlateSolving;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.ViewModel {
    [TestFixture]
    public class PlateSolvingStatusVMTest {

        [Test]
        public async Task ConcurrencyTest() {
            var iterations = 1000;
            var status = new PlateSolvingStatusVM();
            var l = new List<PlateSolveResult>();
            for (int i = 0; i < iterations; i++) {
                l.Add(new PlateSolveResult(new DateTime(2023, 01, 01, 18, 0, 0) + TimeSpan.FromSeconds(i)));
                l.Add(new PlateSolveResult(new DateTime(2023, 01, 01, 18, 0, 0) + TimeSpan.FromSeconds(i)));
            }

            Parallel.For(0, iterations, idx => {
                status.Progress.Report(new PlateSolveProgress() { PlateSolveResult = l[idx] });
                status.Progress.Report(new PlateSolveProgress() { PlateSolveResult = l[idx + iterations] });
            });

            await Task.Delay(500);

            status.PlateSolveHistory.Count.Should().Be(iterations);
        }
    }
}
