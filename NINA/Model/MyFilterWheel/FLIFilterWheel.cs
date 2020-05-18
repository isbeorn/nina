#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Copyright (c) 2019 Dale Ghent <daleg@elemental.org> All rights reserved.
 */

#endregion "copyright"

using FLI;
using NINA.Profile;
using NINA.Utility;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {

    public class FLIFilterWheel : BaseINPC, IFilterWheel {
        private uint FWheelH;
        private bool _connected = false;
        private LibFLI.FLIFilterWheelInfo Info;
        private IProfileService profileService;

        public FLIFilterWheel(string fwheel, IProfileService profileService) {
            this.profileService = profileService;
            string[] fwheelInfo;

            fwheelInfo = fwheel.Split(';');
            Info.Id = fwheelInfo[0];
            Info.Model = fwheelInfo[1];

            Logger.Debug($"FLI: Found filter wheel: {Description}");
        }

        public string Category { get; } = "Finger Lakes Instrumentation";

        public string Id => Info.Id;

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                uint fwheelPostions = 0;
                uint rv;

                if ((rv = LibFLI.FLIOpen(out FWheelH, Info.Id, LibFLI.FLIDomains.DEV_FILTERWHEEL | LibFLI.FLIDomains.IF_USB)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI FWheel: FLIOpen() failed. Returned {rv}");
                    Connected = false;

                    return Connected;
                }

                Logger.Debug($"FLI Wheel: Filter wheel {Info.Id} opened successfully");

                if ((rv = LibFLI.FLIGetFWRevision(FWheelH, out Info.FWrev)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI FWheel: FLIGetFWRevision() failed. Returned {rv}");
                }

                if ((rv = LibFLI.FLIGetHWRevision(FWheelH, out Info.HWrev)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI FWheel: FLIGetHWRevision() failed. Returned {rv}");
                }

                /*
                 * How many filter positions?
                 */
                if ((rv = LibFLI.FLIGetFilterCount(FWheelH, ref fwheelPostions)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI FWheel: FLIGetFilterCount() failed. Returned {rv}");
                    Connected = false;

                    return Connected;
                }
                Info.Positions = fwheelPostions;

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

        public string Name => Info.Model;
        public string Description => string.Format($"{Info.Model} ({Info.Id}) HWRev: {Info.HWrev} FWRev: {Info.FWrev}");

        private string driverInfo = string.Empty;

        public string DriverInfo {
            get {
                StringBuilder version = new StringBuilder(128);

                if (Connected && (string.IsNullOrEmpty(driverInfo))) {
                    if ((LibFLI.FLIGetLibVersion(version, 128)) == 0) {
                        driverInfo = version.ToString();
                    }
                }

                return driverInfo;
            }
        }

        public string DriverVersion => string.Empty;

        public short InterfaceVersion => 1;

        public int[] FocusOffsets => this.Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => this.Filters.Select((x) => x.Name).ToArray();

        public short Position {
            get {
                uint filterPosition = 0;
                uint rv;

                if (Connected) {
                    if ((rv = LibFLI.FLIGetFilterPos(FWheelH, ref filterPosition)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI FWheel: FLIGetFilterPos() failed. Returned {rv}");

                        return -1;
                    }
                }

                return (short)unchecked(filterPosition);
            }
            set {
                long rv;

                if (Connected && (value < Info.Positions)) {
                    if ((rv = LibFLI.FLISetFilterPos(FWheelH, (uint)value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI FWheel: FLISetFilterPos() failed. Returned {rv}");
                    }

                    RaisePropertyChanged();
                }
            }
        }

        public ArrayList SupportedActions => new ArrayList();

        public void Disconnect() {
            Connected = false;

            LibFLI.FLIClose(FWheelH);
        }

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                /*
                 * The issue we must confront here is that there may be too few or too many filters defined in NINA's filter wheel
                 * configuration. Before we grab that list and return it, we first need to make sure that we truncate any extra
                 * filters that were defined, or add any missing ones.
                 *
                 * When removing filters, we will remove entries starting at the end of the list and work back until the number of
                 * filter wheel positions and defined filters match.
                 *
                 * In the case of adding missing filters, FLI filter wheel firmware will provide default names for a given slot. In
                 * this case we just ask the filter wheel that and add it to the collection as an additional FilterInfo entry.
                 *
                 * In the end the user should have the correct number of filter positions listed. It is up to them to maintain the
                 * filter names after that point.
                 */
                var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                int i = filtersList.Count();
                int positions = (int)Info.Positions;
                long rv;

                if (positions < i) {
                    /* Too many filters defined. Truncate the list */
                    for (; i > (int)Info.Positions; i--) {
                        filtersList.RemoveAt(i - 1);
                    }
                } else if (positions > i) {
                    /* Too few filters defined. Add missing filters using the default filter names provided by the filter wheel */
                    for (; i <= positions; i++) {
                        StringBuilder positionName = new StringBuilder(32);

                        if ((rv = LibFLI.FLIGetFilterName(FWheelH, (uint)i, positionName, 32)) != LibFLI.FLI_SUCCESS) {
                            Logger.Error($"FLI FWheel: FLIGetFilterName() failed. Returned {rv}");
                        }

                        var filter = new FilterInfo(positionName.ToString(), 0, (short)i);
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
