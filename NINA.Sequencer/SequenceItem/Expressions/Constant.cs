#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Expressions {
    [ExportMetadata("Name", "Define Local Constant")]
    [ExportMetadata("Description", "Creates a Constant whose value can be used in Expressions")]
    [ExportMetadata("Icon", "ConstantSVG")]
    [ExportMetadata("Category", "Expressions")]
    //[Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public partial class Constant : UserSymbol, IValidatable {

        [ImportingConstructor]
        public Constant() : base() {
            Name = Name;
            Icon = Icon;
        }
        public Constant(Constant copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
            }
        }

        public override object Clone() {
            Constant clone = new Constant(this);

            clone.Identifier = Identifier;
            clone.Expr = new Expression(Expr != null ? Expr.Definition : "", clone.Parent, this);
            return clone;
        }

        public override string ToString() {
            if (Expr != null) {
                return $"Define Constant: {Identifier}, Definition: {Expr.Definition}, Parent: {Parent?.Name} Expr: {Expr}";
            } else {
                return $"Define Constant: {Identifier}, Parent: {Parent?.Name} Expr: null";
            }
        }

        protected ISequenceRootContainer FindRoot() {
            ISequenceContainer p = Parent;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return (ISequenceRootContainer)p;
                }
                p = p.Parent;
            }
            return null;
        }

        public override bool Validate() {
            ISequenceRootContainer root = FindRoot();

            if (root == null) {
                return true;
            }

            IList<string> i = new List<string>();

            if (Identifier.Length == 0 || Expr.Definition.Length == 0) {
                i.Add("A name and a value must be specified");
            } else if (!Regex.IsMatch(Identifier, VALID_SYMBOL)) {
                i.Add("The name of a Constant must be alphanumeric");
            } else if (IsDuplicate) {
                i.Add("The Constant is already defined here; this definition will be ignored.");
            }

            Expression.ValidateExpressions(Issues, Expr);

            foreach (var kvp in Expr.Resolved) {
                if (kvp.Value is Variable) {
                    i.Add("Constant definitions may not include Variables");
                    break;
                }
            }

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Doesn't Execute
            return Task.CompletedTask;
        }

    }
}
