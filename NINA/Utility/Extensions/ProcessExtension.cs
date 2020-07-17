#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Extensions {

    public static class ProcessExtension {

        /// <summary>
        /// Waits asynchronously for the process to exit.
        /// </summary>
        /// <param name="process">The process to wait for cancellation.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A Task representing waiting for the process to end.</returns>
        public static Task WaitForExitAsync(
                this Process process,
                CancellationToken cancellationToken = default(CancellationToken)
        ) {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken)) {
                cancellationToken.Register(() => {
                    tcs.TrySetCanceled();
                    if (!process.HasExited) {
                        process.Kill();
                    }
                });
            }

            return tcs.Task;
        }
    }
}