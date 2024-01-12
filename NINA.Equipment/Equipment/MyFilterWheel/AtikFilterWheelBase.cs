#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model.Equipment;
using NINA.Equipment.SDK.CameraSDKs.AtikSDK;
using NINA.Equipment.Interfaces;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyFilterWheel {

    public abstract class AtikFilterWheelBase : BaseINPC, IFilterWheel {
        protected readonly IProfileService profileService;

        public AtikFilterWheelBase(IProfileService profileService) {
            this.profileService = profileService;
        }

        public short InterfaceVersion => 1;

        public int[] FocusOffsets => Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => Filters.Select((x) => x.Name).ToArray();

        public abstract short Position { get; set; }

        public IList<string> SupportedActions => new List<string>();

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                int i = filtersList.Count;
                int positions = GetEfwPositions();

                if (positions < i) {
                    /* Too many filters defined. Truncate the list */
                    for (; i > positions; i--) {
                        filtersList.RemoveAt(i - 1);
                    }
                } else if (positions > i) {
                    /* Too few filters defined. Add missing filter names using Slot <#> format */
                    for (; i <= positions; i++) {
                        var filter = new FilterInfo(string.Format($"Slot {i}"), 0, (short)i);
                        filtersList.Add(filter);
                    }
                }

                return filtersList;
            }
        }

        protected abstract int GetEfwPositions();

        public bool HasSetupDialog => false;

        public abstract string Id { get; }

        public abstract string Name { get; }

        public string Category => "Atik";

        public abstract bool Connected { get; }

        public abstract string Description { get; }

        public string DriverInfo => AtikCameraDll.DriverName;

        public string DriverVersion => AtikCameraDll.DriverVersion;

        public abstract Task<bool> Connect(CancellationToken token);

        public abstract void Disconnect();

        public void SetupDialog() {
            throw new NotImplementedException();
        }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }
    }
}