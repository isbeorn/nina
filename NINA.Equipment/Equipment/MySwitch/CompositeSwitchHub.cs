#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.Hubs;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace NINA.Equipment.Equipment.MySwitch {
    public class CompositeSwitchHub : EquipmentHub, ISwitchHub {
        public const string DeviceId = "NINA.Switch.Hub";
        private readonly List<SwitchChannel> channels = new List<SwitchChannel>();
        private bool healthy;
        private ICollection<ISwitch> switches = Array.AsReadOnly(Array.Empty<ISwitch>());

        public CompositeSwitchHub(string name) : base(DeviceId, name, "N.I.N.A.") {
        }

        public void Configure(IReadOnlyList<HubDevice> devices) {
            ConfigureDevices(devices);
        }

        public ICollection<ISwitch> Switches => switches;

        protected override bool IsSupportedDevice(IDevice device) => device is ISwitchHub;

        public override bool Connected {
            get {
                if (IsSessionActive && healthy && Devices.Any(x => !ReadConnected(x.Device))) {
                    Fault(null, Loc.Instance["LblEquipmentHubSessionFault"]);
                }

                return IsSessionActive && healthy;
            }
        }

        protected override void SessionConnected() {
            foreach (var child in Devices) {
                var nativeChannels = ((ISwitchHub)child.Device).Switches.ToArray();
                if (nativeChannels.Select(x => x.Id).Distinct().Count() != nativeChannels.Length) {
                    throw new InvalidOperationException(Loc.Instance["LblEquipmentHubInvalidConfiguration"]);
                }

                foreach (var native in nativeChannels) {
                    if (channels.Count > short.MaxValue) {
                        throw new InvalidOperationException(Loc.Instance["LblEquipmentHubInvalidConfiguration"]);
                    }

                    short id = (short)channels.Count;
                    var channel = native is IWritableSwitch writable
                        ? new WritableSwitchChannel(this, child, writable, id, SessionVersion)
                        : new SwitchChannel(this, child, native, id, SessionVersion);
                    channels.Add(channel);
                }
            }

            switches = new ReadOnlyCollection<ISwitch>(channels.Cast<ISwitch>().ToArray());
            healthy = true;
            RaisePropertyChanged(nameof(Switches));
        }

        protected override void SessionEnded() {
            healthy = false;
            foreach (var channel in channels) {
                channel.Detach();
            }

            channels.Clear();
            switches = Array.AsReadOnly(Array.Empty<ISwitch>());
            RaisePropertyChanged(nameof(Switches));
        }

        protected override void ChildChanged(HubDevice child, PropertyChangedEventArgs e) {
            if (!ReadConnected(child.Device)) {
                Fault(child, Loc.Instance["LblEquipmentHubSessionFault"]);
            }

            base.ChildChanged(child, e);
        }

        private void Fault(HubDevice child, string error) {
            healthy = false;
            if (child != null) {
                Report(child, error);
            } else {
                foreach (var disconnected in Devices.Where(x => !ReadConnected(x.Device))) {
                    Report(disconnected, error);
                }
            }

            RaisePropertyChanged(nameof(Connected));
        }

        private void EnsureActive(int version, bool checkConnection) {
            if (version != SessionVersion || !IsSessionActive || !healthy || (checkConnection && !Connected)) {
                throw new InvalidOperationException(Loc.Instance["LblEquipmentHubSessionFault"]);
            }
        }

        private class SwitchChannel : BaseINPC, ISwitch, ICompositeSwitchIdentity {
            protected readonly CompositeSwitchHub Hub;
            protected readonly HubDevice Child;
            protected readonly ISwitch Native;
            protected readonly int Version;
            private readonly INotifyPropertyChanged notifier;
            private bool detached;

            public SwitchChannel(CompositeSwitchHub hub, HubDevice child, ISwitch native, short id, int version) {
                Hub = hub;
                Child = child;
                Native = native;
                Version = version;
                Id = id;
                notifier = native as INotifyPropertyChanged;
                if (notifier != null) {
                    notifier.PropertyChanged += NativeChanged;
                }
            }

            public short Id { get; }

            public string DeviceId => Child.Device.Id;

            public short ChannelId => Native.Id;

            public string Name => string.Format(Loc.Instance["LblHubSwitchChannelName"], Child.SourceNumber, Native.Name);

            public string ExpressionName => $"S{Child.SourceNumber} C{Native.Id + 1} {Native.Name}";

            public string Description => Native.Description;

            public double Value => Native.Value;

            private void NativeChanged(object sender, PropertyChangedEventArgs e) {
                RaisePropertyChanged(e.PropertyName);
            }

            public void Detach() {
                detached = true;
                if (notifier != null) {
                    notifier.PropertyChanged -= NativeChanged;
                }
            }

            protected void EnsureActive(bool checkConnection = true) {
                if (detached) {
                    throw new InvalidOperationException(Loc.Instance["LblEquipmentHubSessionFault"]);
                }

                Hub.EnsureActive(Version, checkConnection);
            }

            public bool Poll() {
                EnsureActive();
                try {
                    if (Native.Poll()) {
                        EnsureActive();
                        return true;
                    }

                    Hub.Fault(Child, Loc.Instance["LblEquipmentHubSessionFault"]);
                } catch (Exception ex) {
                    Hub.Fault(Child, ex.Message);
                }

                return false;
            }
        }

        private sealed class WritableSwitchChannel : SwitchChannel, IWritableSwitch {
            private readonly IWritableSwitch writable;

            public WritableSwitchChannel(CompositeSwitchHub hub, HubDevice child, IWritableSwitch native, short id, int version)
                : base(hub, child, native, id, version) {
                writable = native;
            }

            public double Maximum => writable.Maximum;

            public double Minimum => writable.Minimum;

            public double StepSize => writable.StepSize;

            public double TargetValue {
                get => writable.TargetValue;
                set {
                    EnsureActive(checkConnection: false);
                    writable.TargetValue = value;
                }
            }

            public void SetValue() {
                EnsureActive();
                writable.SetValue();
            }
        }
    }
}
