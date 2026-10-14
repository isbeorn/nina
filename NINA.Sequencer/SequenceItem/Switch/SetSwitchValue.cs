#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.Switch {

    [ExportMetadata("Name", "Lbl_SequenceItem_Switch_SetSwitchValue_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Switch_SetSwitchValue_Description")]
    [ExportMetadata("Icon", "ButtonSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Switch")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions(GenerateValidation = true)]

    public partial class SetSwitchValue : SequenceItem, IValidatable {
        private ISwitchMediator switchMediator;

        [ImportingConstructor]
        public SetSwitchValue(ISwitchMediator switchMediator) {
            this.switchMediator = switchMediator;

            WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(CreateDummyList());
            RefreshSelection(WritableSwitches.First());
        }

        private SetSwitchValue(SetSwitchValue cloneMe) : this(cloneMe.switchMediator) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(SetSwitchValue clone) {
            clone.SwitchIndex = SwitchIndex;
            clone.HubDeviceId = HubDeviceId;
            clone.HubChannelId = HubChannelId;
        }

        [IsExpression (Default = 1)]
        public partial double Value { get; set; }

        private short switchIndex;

        [JsonProperty]
        public short SwitchIndex {
            get => switchIndex;
            set {
                if (value > -1) {
                    switchIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

#nullable enable annotations
        [ObservableProperty]
        [property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        private string? hubDeviceId;

        [ObservableProperty]
        [property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        private short? hubChannelId;
#nullable restore annotations

        private bool HasStableIdentity => HubDeviceId != null || HubChannelId.HasValue;

        private IWritableSwitch selectedSwitch;

        [JsonIgnore]
        public IWritableSwitch SelectedSwitch {
            get => selectedSwitch;
            set {
                selectedSwitch = value;
                var info = switchMediator.GetInfo();
                if (value != null && !(value is DummySwitch) && info?.Connected == true
                    && info.WritableSwitches?.Contains(value) == true) {
                    SwitchIndex = (short)info.WritableSwitches.IndexOf(value);
                    CaptureIdentity(value, info);
                } else if (value is DummySwitch && !HasStableIdentity) {
                    SwitchIndex = (short)(WritableSwitches?.IndexOf(value) ?? -1);
                }

                RaisePropertyChanged();
            }
        }

        private ReadOnlyCollection<IWritableSwitch> writableSwitches;

        public ReadOnlyCollection<IWritableSwitch> WritableSwitches {
            get => writableSwitches;
            set {
                writableSwitches = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            ResolveConnectedSwitch();
            var value = Value;
            if (HasStableIdentity) {
                try {
                    await switchMediator.SetSwitchValueByIdentity(HubDeviceId, HubChannelId.Value, value, progress, token);
                    return;
                } catch (NotSupportedException) {
                    token.ThrowIfCancellationRequested();
                    ResolveConnectedSwitch();
                }
            }

#pragma warning disable CS0618 // Compatibility fallback when identity metadata or the identity API is unavailable.
            await switchMediator.SetSwitchValue(switchIndex, value, progress, token);
#pragma warning restore CS0618
        }

        private IWritableSwitch ResolveConnectedSwitch() {
            var info = switchMediator.GetInfo();
            if (info?.Connected != true) {
                throw new SequenceEntityFailedException(Loc.Instance["LblSwitchNotConnected"]);
            }

            var target = ResolveSwitch(info);
            if (target == null) {
                throw new SequenceEntityFailedException(Loc.Instance[HasStableIdentity
                    ? "LblHubSwitchTargetInvalid" : "Lbl_SequenceItem_Validation_NoSwitchSelected"]);
            }

            return target;
        }

        private void CaptureIdentity(IWritableSwitch target, SwitchInfo info) {
            if (target is ICompositeSwitchIdentity identity) {
                HubDeviceId = identity.DeviceId;
                HubChannelId = identity.ChannelId;
            } else if (!string.IsNullOrEmpty(info.DeviceId)) {
                HubDeviceId = info.DeviceId;
                HubChannelId = target.Id;
            } else {
                HubDeviceId = null;
                HubChannelId = null;
            }
        }

        private IWritableSwitch ResolveSwitch(SwitchInfo info) {
            WritableSwitches = info.WritableSwitches ?? new ReadOnlyCollection<IWritableSwitch>(Array.Empty<IWritableSwitch>());
            IWritableSwitch target = null;
            if (!HasStableIdentity && switchIndex >= 0 && switchIndex < WritableSwitches.Count) {
                target = WritableSwitches[switchIndex];
                if (!(target is DummySwitch)) {
                    CaptureIdentity(target, info);
                }
            }

            if (HasStableIdentity) {
                target = null;
                if (!string.IsNullOrEmpty(HubDeviceId) && HubChannelId.HasValue) {
                    var matches = WritableSwitches.Cast<ISwitch>()
                        .Concat(info.ReadonlySwitches ?? Enumerable.Empty<ISwitch>())
                        .Where(s => s is ICompositeSwitchIdentity identity
                            ? string.Equals(identity.DeviceId, HubDeviceId, StringComparison.Ordinal) && identity.ChannelId == HubChannelId.Value
                            : string.Equals(info.DeviceId, HubDeviceId, StringComparison.Ordinal) && s.Id == HubChannelId.Value)
                        .ToList();
                    if (matches.Count == 1 && matches[0] is IWritableSwitch writable && WritableSwitches.Contains(writable)) {
                        target = writable;
                        SwitchIndex = (short)WritableSwitches.IndexOf(target);
                    }
                }
            }

            RefreshSelection(target);
            return target;
        }

        private void RefreshSelection(IWritableSwitch target) {
            selectedSwitch = target;
            RaisePropertyChanged(nameof(SelectedSwitch));
        }

        private IList<IWritableSwitch> CreateDummyList() {
            var dummySwitches = new List<IWritableSwitch>();
            for (short i = 0; i < 20; i++) {
                dummySwitches.Add(new DummySwitch((short)(i + 1)));
            }
            return dummySwitches;        
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        partial void ValidateAdditional(IList<string> issues) {
            try {
                var info = switchMediator.GetInfo();
                IWritableSwitch target;
                if (info?.Connected != true) {
                    WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(CreateDummyList());
                    target = !HasStableIdentity && switchIndex >= 0 && switchIndex < WritableSwitches.Count
                        ? WritableSwitches[switchIndex] : null;
                    RefreshSelection(target);
                    issues.Add(Loc.Instance["LblSwitchNotConnected"]);
                } else {
                    target = ResolveSwitch(info);
                    if (target == null) {
                        issues.Add(Loc.Instance[HasStableIdentity
                            ? "LblHubSwitchTargetInvalid"
                            : WritableSwitches.Count == 0
                                ? "Lbl_SequenceItem_Validation_NoWritableSwitch"
                                : "Lbl_SequenceItem_Validation_NoSwitchSelected"]);
                    }
                }

                if (target != null && (Value < target.Minimum || Value > target.Maximum)) {
                    issues.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Validation_InvalidSwitchValue"], target.Minimum, target.Maximum, target.StepSize));
                }
            } catch (Exception ex) {
                issues.Add(Loc.Instance["LblHubSwitchTargetInvalid"]);
                Logger.Error(ex);
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetSwitchValue)}, SwitchIndex {SwitchIndex}, Value: {Value}";
        }
    }
}