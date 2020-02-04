#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

/*
 * Copyright (c) 2019 Dale Ghent <daleg@elemental.org> All rights reserved.
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

            Info.Name = string.Format($"{cameraModel.ToString()} {Info.Positions}-Slot Filter Wheel");

            LibQHYCCD.N_CloseQHYCCD(FWheelP);

            Logger.Debug($"QHYCFW: Found filter wheel: {Name}");
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                byte[] position = new byte[1];

                Logger.Debug($"QHYCFW: Connecting to filter wheel {Name}");
                FWheelP = LibQHYCCD.N_OpenQHYCCD(Info.Id);

                _ = LibQHYCCD.GetQHYCCDCFWStatus(FWheelP, position);
                Info.Position = Convert.ToInt16(string.Join("", Encoding.ASCII.GetChars(position)));

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
                Logger.Debug($"QHYCFW: Current postion: {Info.Position}");
                return Info.Position;
            }
            set {
                byte[] status = new byte[1];
                string position = value.ToString("X1");

                Logger.Debug($"QHYCFW: Moving to position {value} (str: {position})");

                if (LibQHYCCD.SendOrder2QHYCCDCFW(FWheelP, position, position.Length) != LibQHYCCD.QHYCCD_SUCCESS) {
                    Logger.Error($"QHYCFW: Failed to order move to position {value} (str: {position})!");
                    return;
                }

                /*
                 * SendOrder2QHYCCDCFW() does not block, so we do not want the Position property set to return too early because
                 * the filter wheel might still be in motion with NINA under the belief that the selected filter is in place
                 * and begins the next image exposure.
                 *
                 * To address this, we repeatedly check the status of the filter wheel and sleep for 250ms between checks if the current position
                 * of the filter wheel does not match the selected position (ie, the filter wheel is still in motion.)
                 */
                _ = LibQHYCCD.GetQHYCCDCFWStatus(FWheelP, status);

                while (string.Join("", Encoding.ASCII.GetChars(status)) != position) {
                    Logger.Debug($"QHYCFW: Currently moving from position {Info.Position} to position {value} (str: {position}). Current status: {string.Join("", Encoding.ASCII.GetChars(status))})");
                    Task.Delay(250);

                    _ = LibQHYCCD.GetQHYCCDCFWStatus(FWheelP, status);
                }

                Info.Position = value;
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