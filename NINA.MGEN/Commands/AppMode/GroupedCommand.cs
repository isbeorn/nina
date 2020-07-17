#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FTD2XX_NET;
using NINA.MGEN.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.MGEN.Commands.AppMode {

    public abstract class GroupedCommand<TResult> : AppModeCommand<TResult> where TResult : IMGENResult {
        public abstract byte SubCommandCode { get; }

        protected abstract TResult ExecuteSubCommand(IFTDI device);

        public override TResult Execute(IFTDI device) {
            Write(device, CommandCode);
            var data = Read(device, 1);

            if (data[0] == AcknowledgeCode) {
                // Autoguiding Command intitialized - Sub Command can follow
                return ExecuteSubCommand(device);
            } else {
                throw new UnexpectedReturnCodeException();
            }
        }
    }
}