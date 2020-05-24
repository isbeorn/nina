#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility;
using QHYCCD;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {

    public class QHYFilterWheel : BaseINPC, IFilterWheel {
        private IntPtr FWheelP;
        private LibQHYCCD.QHYCCD_FILTER_WHEEL_INFO Info;
        private bool _connected = false;
        private IProfileService profileService;

        public QHYFilterWheel(string fwheel, IProfileService profileService) {
            this.profileService = profileService;
            StringBuilder FWheelId = new StringBuilder(32);
            StringBuilder cameraModel = new StringBuilder(0);

            FWheelId.Append(fwheel);
            LibQHYCCD.N_GetQHYCCDModel(FWheelId, cameraModel);

            Info.Id = FWheelId;

            FWheelP = LibQHYCCD.N_OpenQHYCCD(Info.Id);

            if (LibQHYCCD.IsQHYCCDCFWPlugged(FWheelP) == LibQHYCCD.QHYCCD_SUCCESS) {
                Info.Positions = (uint)LibQHYCCD.GetQHYCCDParam(FWheelP, LibQHYCCD.CONTROL_ID.CONTROL_CFWSLOTSNUM);
            } else {
                Logger.Error($"QHYCFW: {Id} suddenly has no filter wheel!");
                return;
            }

            Info.Name = string.Format($"{cameraModel} {Info.Positions}-Slot Filter Wheel");

            LibQHYCCD.N_CloseQHYCCD(FWheelP);

            Logger.Debug($"QHYCFW: Found filter wheel: {Name}");
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                byte[] position = new byte[1];

                Logger.Debug($"QHYCFW: Connecting to filter wheel {Name}");
                FWheelP = LibQHYCCD.N_OpenQHYCCD(Info.Id);

                Connected = true;
                return Connected;
            });
        }

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public string Id => Info.Id.ToString();
        public string Name => Info.Name;
        public string Category => "QHYCCD";
        public string Description => string.Format($"Integrated or 4-pin Filter Wheel on {Info.Id}");
        public string DriverInfo => "Native driver for QHY integrated or 4-pin filter wheels";
        public string DriverVersion => "1.0";
        public short InterfaceVersion => 1;

        public int[] FocusOffsets => this.Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => this.Filters.Select((x) => x.Name).ToArray();

        public short Position {
            get {
                uint rv;
                byte[] status = new byte[1];
                short position;
                string statusString;

                if ((rv = LibQHYCCD.GetQHYCCDCFWStatus(FWheelP, status)) != LibQHYCCD.QHYCCD_SUCCESS) {
                    Logger.Error($"QHYCFW: Failed to get filter wheel position: {rv}");
                    return -1;
                }

                statusString = string.Join("", Encoding.ASCII.GetChars(status));
                Logger.Debug($"QHYCFW: Current position: {statusString}");

                /*
                 * GetQHYCCDCFWStatus() returns a status of "N" while the filter wheel is in motion. Return -1 in this case per the ASCOM specification
                 */
                if (statusString == "N") {
                    position = -1;
                } else {
                    short.TryParse(statusString, out position);
                }

                return position;
            }
            set {
                string position = value.ToString("X1");

                Logger.Debug($"QHYCFW: Moving to position {value} (str: {position})");

                if (LibQHYCCD.SendOrder2QHYCCDCFW(FWheelP, position, position.Length) != LibQHYCCD.QHYCCD_SUCCESS) {
                    Logger.Error($"QHYCFW: Failed to order move to position {value} (str: {position})!");
                    return;
                }

                RaisePropertyChanged();
            }
        }

        public ArrayList SupportedActions => new ArrayList();

        public void Disconnect() {
            Logger.Debug($"QHYCFW: Closing filter wheel {Name}");

            Connected = false;
            LibQHYCCD.N_CloseQHYCCD(FWheelP);
            FWheelP = IntPtr.Zero;
        }

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                int i = filtersList.Count();
                int positions = (int)Info.Positions;

                if (positions < i) {
                    /* Too many filters defined. Truncate the list */
                    for (; i > (int)Info.Positions; i--) {
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

        public bool HasSetupDialog => false;

        public void SetupDialog() {
        }
    }
}