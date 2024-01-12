#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Container.ExecutionStrategy {

    public class ParallelStrategy : IExecutionStrategy {

        public object Clone() {
            return new ParallelStrategy();
        }

        public Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            progress?.Report(new ApplicationStatus() {
                Status = Loc.Instance["LblExecutingItemsInParallel"]
            });

            var tasks = new List<Task>();
            var items = context.GetItemsSnapshot();
            foreach (var item in items) {
                if(item.Status != Core.Enum.SequenceEntityStatus.DISABLED) { 
                    var itemProgress = new Progress<ApplicationStatus>((p) => {
                        p.Source = item.Name;
                        progress?.Report(p);
                    });
                    tasks.Add(item.Run(itemProgress, token));
                }
            }
            return Task.WhenAll(tasks);
        }
    }
}