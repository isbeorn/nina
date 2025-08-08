#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FLI;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Interfaces;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyFilterWheel {

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

        public string Id => $"{Info.Model}#{Info.Id}";

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
        public string DisplayName => Name;
        public string Description => string.Format($"{Info.Model} ({Info.Id}) HWRev: {Info.HWrev} FWRev: {Info.FWrev}");

        private string driverInfo = string.Empty;

        public string DriverInfo {
            get {
                StringBuilder version = new StringBuilder(128);

                if (Connected && string.IsNullOrEmpty(driverInfo)) {
                    if (LibFLI.FLIGetLibVersion(version, 128) == 0) {
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

        public IList<string> SupportedActions => new List<string>();

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
                int i = filtersList.Count;
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

                        if (string.IsNullOrEmpty(positionName.ToString())) {
                            break;
                        }

                        string filterName;

                        if (Info.Model.Contains("CenterLine")) {
                            var regex = @"(Empty|^$)";

                            var filterNames = positionName.ToString().Split('/');
                            var wheel0 = filterNames[0];
                            var wheel1 = filterNames[1];

                            var isWheel0Empty = Regex.Match(wheel0, regex, RegexOptions.IgnoreCase);
                            var isWheel1Empty = Regex.Match(wheel1, regex, RegexOptions.IgnoreCase);

                            if (isWheel0Empty.Success && isWheel1Empty.Success) {
                                var slot0 = "Empty";
                                var slot1 = "Empty";

                                if (!string.IsNullOrEmpty(wheel0)) {
                                    slot0 = wheel0;
                                }

                                if (!string.IsNullOrEmpty(wheel1)) {
                                    slot1 = wheel1;
                                }

                                filtersList.Add(new FilterInfo($"{slot0}/{slot1}", 0, (short)i));
                                continue;
                            }

                            wheel0 = Regex.Replace(wheel0, regex, string.Empty, RegexOptions.IgnoreCase);
                            wheel1 = Regex.Replace(wheel1, regex, string.Empty, RegexOptions.IgnoreCase);

                            if (string.IsNullOrEmpty(wheel0)) {
                                filterName = wheel1;
                            } else if (string.IsNullOrEmpty(wheel1)) {
                                filterName = wheel0;
                            } else {
                                filterName = $"{wheel0}/{wheel1}";
                            }

                        } else {
                            filterName = positionName.ToString();
                        }

                        filtersList.Add(new FilterInfo(filterName, 0, (short)i));
                    }
                }

                return filtersList;
            }
        }

        public bool HasSetupDialog => false;

        public void SetupDialog() {
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