#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Utility.Extensions {
    public static class DelegateExtension {
        public static async Task InvokeAsync<TArgs>(this Func<object, TArgs, Task> func, object sender, TArgs e) {
            if (func == null) {
                return;
            }

            var invocationList = func.GetInvocationList().Cast<Func<object, TArgs, Task>>();
            var tasks = invocationList.Select(async f =>
            {
                var stopwatch = Stopwatch.StartNew();
                try {
                    await f(sender, e);
                } catch(Exception ex) {
                    Logger.Error(ex);
                }                
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 1000) {
                    MethodInfo methodInfo = f.GetMethodInfo();
                    string fullMethodName = $"{methodInfo.DeclaringType.FullName}.{methodInfo.Name}";
                    Logger.Warning($"Eventhandler {fullMethodName} took {stopwatch.ElapsedMilliseconds} ms to execute.");
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
