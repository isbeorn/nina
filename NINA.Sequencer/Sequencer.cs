#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Focuser;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Trigger;
using NINA.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Validations;
using NINA.Core.MyMessageBox;
using NINA.Core.Locale;
using NINA.Sequencer.SequenceItem;

namespace NINA.Sequencer {

    public class Sequencer : BaseINPC, ISequencer {

        public Sequencer(
            ISequenceRootContainer sequenceRootContainer
        ) {
            MainContainer = sequenceRootContainer;
        }
        // This is a hack to utilize the TreeView control. The Items will just point at the single item in the sequencer which is the root node in the tree
        public List<ISequenceRootContainer> Items => new List<ISequenceRootContainer> { MainContainer };

        private ISequenceRootContainer mainContainer;

        public ISequenceRootContainer MainContainer {
            get => mainContainer;
            set {
                if(mainContainer != null && mainContainer != value){
                    bool saveChangeStatus = mainContainer.HasChanges["*"];
                    // when a new sequence is loaded, allow existing sequence elements to detect that
                    // they are no longer part of the sequence root container.
                    foreach (var item in mainContainer.GetItemsSnapshot()){
                        item.Detach();
                    }
                    mainContainer.HasChanges["*"] = saveChangeStatus;
                }
                mainContainer = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Items));
            }
        }


        public Task Start(IProgress<ApplicationStatus> progress, CancellationToken token, bool skipIssuePrompt) {
            return Task.Run(async () => {
                if (!skipIssuePrompt && !PromptForIssues()) {
                    return false;
                }
                try {
                    Initialize(MainContainer);
                    await MainContainer.Run(progress, token);
                } catch (OperationCanceledException) {
                    Logger.Info("Sequence run was cancelled");
                }

                Teardown(MainContainer);

                return true;
            });
        }

        public Task Start(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Start(progress, token, false);
        }

        private bool PromptForIssues() {
            var issues = Validate(MainContainer).Distinct();

            if (issues.Any()) {
                var builder = new StringBuilder();
                builder.AppendLine(Loc.Instance["LblPreSequenceChecklist"]).AppendLine();

                foreach (var issue in issues) {
                    builder.Append("  - ");
                    builder.AppendLine(issue);
                }

                builder.AppendLine();
                builder.Append(Loc.Instance["LblStartSequenceAnyway"]);

                var diag = MyMessageBox.Show(
                    builder.ToString(),
                    Loc.Instance["LblPreSequenceChecklistHeader"],
                    System.Windows.MessageBoxButton.OKCancel,
                    System.Windows.MessageBoxResult.Cancel
                );
                if (diag == System.Windows.MessageBoxResult.Cancel) {
                    return false;
                }
            }
            return true;
        }

        private void Initialize(ISequenceContainer context) {
            if (context != null) {
                var conditionable = context as IConditionable;
                if (conditionable != null) {
                    foreach (var condition in conditionable.GetConditionsSnapshot()) {
                        condition.Initialize();
                    }
                }
                var triggerable = context as ITriggerable;
                if (triggerable != null) {
                    foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                        trigger.Initialize();
                    }
                }

                foreach (var item in context.GetItemsSnapshot()) {
                    item.Initialize();
                    if (item is ISequenceContainer) {
                        var container = item as ISequenceContainer;
                        Initialize(container);
                    }
                }
            }
        }

        private void Teardown(ISequenceContainer context) {
            if (context != null) {
                var conditionable = context as IConditionable;
                if (conditionable != null) {
                    foreach (var condition in conditionable.GetConditionsSnapshot()) {
                        condition.Teardown();
                    }
                }
                var triggerable = context as ITriggerable;
                if (triggerable != null) {
                    foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                        trigger.Teardown();
                    }
                }

                foreach (var item in context.GetItemsSnapshot()) {
                    item.Teardown();
                    if (item is ISequenceContainer) {
                        var container = item as ISequenceContainer;
                        Teardown(container);
                    }
                }
            }
        }

        private IList<string> Validate(ISequenceContainer container) {
            List<string> issues = new List<string>();

            if (container is IConditionable conditionable) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    if (condition.Status != Core.Enum.SequenceEntityStatus.DISABLED && condition is IValidatable) {
                        var v = condition as IValidatable;
                        v.Validate();
                        issues.AddRange(v.Issues);
                    }
                }
            }

            if (container is ITriggerable triggerable) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    if (trigger.Status != Core.Enum.SequenceEntityStatus.DISABLED && trigger is IValidatable) {
                        var v = trigger as IValidatable;
                        v.Validate();
                        issues.AddRange(v.Issues);
                    }
                }
            }

            foreach (var item in container.GetItemsSnapshot()) {
                if (item.Status != Core.Enum.SequenceEntityStatus.DISABLED && item is IValidatable) {
                    var v = item as IValidatable;
                    v.Validate();
                    issues.AddRange(v.Issues);
                }

                if (item is ISequenceContainer && !(item is IImmutableContainer) && item.Status != Core.Enum.SequenceEntityStatus.DISABLED) {
                    // The immutablecontainer is excluded as it will itself validate the things of its children
                    issues.AddRange(Validate(item as ISequenceContainer));
                }
            }
            return issues;
        }
    }
}