#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.INDI.Interfaces {
    public interface IINDIDevice {
        string Id { get; }
        string Name { get; }
        string DisplayName { get; }
        string Category { get; }
        bool Connected { get; set; }
        string Description { get; }
        string DriverInfo { get; }
        string DriverVersion { get; }

        Task<bool> Connect(CancellationToken ct);
        void Disconnect();
        void Dispose();

        IList<string> SupportedActions { get; }
        string Action(string actionName, string actionParameters);
        void CommandBlind(string command, bool raw = false);
        bool CommandBool(string command, bool raw = false);
        string CommandString(string command, bool raw = false);
    }
}
