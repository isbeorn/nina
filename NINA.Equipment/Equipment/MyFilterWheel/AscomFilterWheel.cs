#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Common.DeviceInterfaces;
using ASCOM.Com.DriverAccess;
using NINA.Core.Locale;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASCOM.Alpaca.Discovery;

namespace NINA.Equipment.Equipment.MyFilterWheel {

    internal class AscomFilterWheel : AscomDevice<IFilterWheelV3>, IFilterWheel, IDisposable {

        public AscomFilterWheel(string filterWheelId, string name, IProfileService profileService) : base(filterWheelId, name) {
            this.profileService = profileService;
        }
        public AscomFilterWheel(AscomDevice deviceMeta, IProfileService profileService) : base(deviceMeta) {
            this.profileService = profileService;
        }

        public int[] FocusOffsets => GetProperty(nameof(FilterWheel.FocusOffsets), new int[] { });

        public string[] Names => GetProperty(nameof(FilterWheel.Names), new string[] { });

        public short Position {
            get => GetProperty<short>(nameof(FilterWheel.Position), -1);
            set => SetProperty(nameof(Focuser.Position), value);
        }

        private IProfileService profileService;

        public AsyncObservableCollection<FilterInfo> Filters => profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;

        protected override string ConnectionLostMessage => Loc.Instance["LblFilterwheelConnectionLost"];

        protected override Task PostConnect() {
            var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;

            // Find duplicate positions due to data corruption and remove duplicates
            var duplicates = filtersList.GroupBy(x => x.Position).Where(x => x.Count() > 1).ToList();
            foreach(var group in duplicates) {
                foreach(var filterToRemove in group) {
                    Logger.Warning($"Duplicate filter position defined in filter list. Removing the duplicates and importing from filter wheel again. Removing filter: {filterToRemove.Name}, focus offset: {filterToRemove.FocusOffset}");
                    filtersList.Remove(filterToRemove);
                }
            }
            
            if(filtersList.Count > 0) { 
                // Scan for missing position indexes between 0 .. maxPosition and reimport them
                var existingPositions = filtersList.Select(x => (int)x.Position).ToList();
                var missingPositions = Enumerable.Range(0, existingPositions.Max()).Except(existingPositions);
                foreach(var position in missingPositions) {
                    if(device.Names.Length > position) {
                        var offset = device.FocusOffsets.Length > position ? device.FocusOffsets[position] : 0;
                        var filterToAdd = new FilterInfo(device.Names[position], offset, (short)position);
                        Logger.Warning($"Missing filter position. Importing filter: {filterToAdd.Name}, focus offset: {filterToAdd.FocusOffset}");
                        filtersList.Insert(position, filterToAdd);
                    }
                }
            }


            int profileFilters = filtersList.Count;
            var deviceFilters = device.Names.Length;

            if (profileFilters < deviceFilters) {
                /* Not enough filters defined. Add missing to the list */
                for (int i = profileFilters; i < deviceFilters; i++) {
                    var offset = device.FocusOffsets.Length > i ? device.FocusOffsets[i] : 0;
                    var filter = new FilterInfo(device.Names[i], offset, (short)i);
                    Logger.Info($"Not enough filters defined in the equipment filter list. Importing filter: {filter.Name}, focus offset: {filter.FocusOffset}");
                    filtersList.Add(filter);
                }
            } else if (profileFilters > deviceFilters) {
                /* Too many filters defined. Truncate the list */
                for (int i = profileFilters - 1; i >= deviceFilters; i--) {
                    var filterToRemove = filtersList[i];
                    Logger.Warning($"Too many filters defined in the equipment filter list. Removing filter: {filterToRemove.Name}, focus offset: {filterToRemove.FocusOffset}");
                    filtersList.Remove(filterToRemove);
                }
            }
            profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters = filtersList;
            return Task.CompletedTask;
        }

        protected override IFilterWheelV3 GetInstance() {
            if (!IsAlpacaDevice()) {
                return new FilterWheel(Id);
            } else {
                return new ASCOM.Alpaca.Clients.AlpacaFilterWheel(deviceMeta.ServiceType, deviceMeta.IpAddress, deviceMeta.IpPort, deviceMeta.AlpacaDeviceNumber, false, null);
            }
        }
    }
}