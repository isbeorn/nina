#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Model {

    public class TooltipDescriptionAttribute : DescriptionAttribute {

        public TooltipDescriptionAttribute(string descriptionLabel, string tooltipLabel)
            : base(descriptionLabel) {
            this.TooltipLabelValue = tooltipLabel;
        }

        public virtual string TooltipLabel => TooltipLabelValue;

        protected string TooltipLabelValue { get; set; }
    }
}