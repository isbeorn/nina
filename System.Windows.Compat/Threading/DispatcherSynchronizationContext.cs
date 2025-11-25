#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace System.Windows.Threading
{
    public class DispatcherSynchronizationContext : System.Threading.SynchronizationContext
    {
        private readonly Dispatcher _dispatcher;

        public DispatcherSynchronizationContext(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public override void Send(System.Threading.SendOrPostCallback d, object state)
        {
            _dispatcher.Invoke(() => d(state));
        }

        public override void Post(System.Threading.SendOrPostCallback d, object state)
        {
            _dispatcher.BeginInvoke(() => d(state));
        }
    }
}
