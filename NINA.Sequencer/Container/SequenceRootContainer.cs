#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Extensions;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINA.Sequencer.Container {

    [ExportMetadata("Name", "Lbl_SequenceContainer_SequenceRootContainer_Name")]
    [ExportMetadata("Description", "Lbl_SequenceContainer_SequentialContainer_Description")]
    [ExportMetadata("Icon", "BoxSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SequenceRootContainer : SequenceContainer, ISequenceRootContainer, IImmutableContainer {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
        }

        [ImportingConstructor]
        public SequenceRootContainer() : base(new SequentialStrategy()) {
        }

        public override ICommand ResetProgressCommand => new GalaSoft.MvvmLight.Command.RelayCommand<object>(
            (o) => {
                if (MyMessageBox.Show(Loc.Instance["Lbl_SequenceContainer_SequenceRootContainer_ResetPrompt"], Loc.Instance["Lbl_SequenceContainer_SequenceRootContainer_ResetPromptCaption"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                    base.ResetProgressCommand.Execute(o);
                }
            }
        );

        public override ICommand DetachCommand => new GalaSoft.MvvmLight.Command.RelayCommand<object>(
            (o) => {
                if (MyMessageBox.Show(Loc.Instance["Lbl_SequenceContainer_SequenceRootContainer_ClearPrompt"], Loc.Instance["Lbl_SequenceContainer_SequenceRootContainer_ClearCaption"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                    foreach (var trigger in GetTriggersSnapshot()) {
                        trigger.Detach();
                    }
                    SequenceTitle = Loc.Instance["Lbl_SequenceContainer_SequenceRootContainer_Name"];
                    ClearContainer(Items[0] as ISequenceContainer);
                    ClearContainer(Items[1] as ISequenceContainer);
                    ClearContainer(Items[2] as ISequenceContainer);
                    GC.Collect(2);
                }
            }
        );

        private void ClearContainer(ISequenceContainer container) {
            foreach (var item in container.GetItemsSnapshot()) {
                item.Detach();
            }
        }

        private object runningItemsLock = new object();
        private List<ISequenceItem> runningItems = new List<ISequenceItem>();

        public void AddRunningItem(ISequenceItem item) {
            lock (runningItemsLock) {
                runningItems.Add(item);
            }
        }

        public void RemoveRunningItem(ISequenceItem item) {
            lock (runningItemsLock) {
                runningItems.Remove(item);
            }
        }

        public void SkipCurrentRunningItems() {
            lock (runningItemsLock) {
                foreach (var item in runningItems) {
                    item.Skip();
                }
            }
        }

        public IReadOnlyCollection<ISequenceItem> GetCurrentRunningItems() {
            lock (runningItemsLock) {
                return runningItems.ToList().AsReadOnly();
            }
        }

        public event Func<object, SequenceEntityFailureEventArgs, Task> FailureEvent;

        public async Task RaiseFailureEvent(ISequenceEntity sender, Exception ex) {
            try {                
                await (FailureEvent?.InvokeAsync(sender, new SequenceEntityFailureEventArgs(sender, ex)) ?? Task.CompletedTask);
            } catch(Exception eventException) {
                Logger.Error(eventException);
            }
        }

        public override ICommand DropIntoCommand => new GalaSoft.MvvmLight.Command.RelayCommand<object>((o) => {
            (Items[1] as TargetAreaContainer).DropIntoCommand.Execute(o);
        });

        public override object Clone() {
            return new SequenceRootContainer() {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        private string sequenceTitle;

        public string SequenceTitle {
            get => string.IsNullOrEmpty(sequenceTitle) ? Name : sequenceTitle;
            set {
                if (sequenceTitle != value) {
                    sequenceTitle = value;
                    RaisePropertyChanged();
                    Name = value;
                }
            }
        }

        public Dictionary<string, bool> HasChanges { get => hasChanges; }

        private Dictionary<string, bool> hasChanges = new Dictionary<string, bool>() { { defaultChangeSet, false } };

        public bool DoesHaveChanges(string hasChangeSet) {
            return HasChanges.ContainsKey(hasChangeSet) && HasChanges[hasChangeSet];
        }
        public void SetChanged(string changedSet=defaultChangeSet) {
            if (HasChanges.ContainsKey(changedSet))
                HasChanges[changedSet] = true;
            else
                HasChanges.Add(changedSet, true);
        }

        /*        public void ClearHasChanged() {
                    foreach (string key in HasChanges.Keys) {
                        HasChanges[key] = false;
                    }
                }

                public bool ShouldStopForChanges(string name, string hasChangedSet) {
                    if ((HasChanges.ContainsKey(hasChangedSet)) && (HasChanges[hasChangedSet]) &&
                        (MyMessageBox.Show(
                            string.Format(Loc.Instance["LblChangedSequenceWarning"], name ?? ""),
                            Loc.Instance["LblChangedSequenceWarningTitle"],
                            MessageBoxButton.YesNo, MessageBoxResult.Yes) == MessageBoxResult.No)) {
                        return true;
                    }
                    return false;
                }

        */

    }
}