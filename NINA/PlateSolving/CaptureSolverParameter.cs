using System;

namespace NINA.PlateSolving {
    public class CaptureSolverParameter : PlateSolveParameter {
        public int Attempts { get; set; }
        public TimeSpan ReattemptDelay { get; set; }
    }
}