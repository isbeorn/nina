#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using BenchmarkDotNet.Running;

namespace NINA.Benchmark {

    internal static class Program {

        [STAThread]
        private static void Main(string[] args) {
            if (args.Length == 2 && args[0] == "--target-diagnostics") {
                TargetPredictionDiagnostics.Write(args[1]);
                return;
            }

            if (args.Length == 2 && args[0] == "--horizon-file-diagnostics") {
                HorizonFileDiagnostics.Write(args[1]);
                return;
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
