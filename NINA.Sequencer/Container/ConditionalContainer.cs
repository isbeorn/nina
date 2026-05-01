#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.Validations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace NINA.Sequencer.Container {

    [ExportMetadata("Name", "Lbl_SequenceContainer_ConditionalContainer_Name")]
    [ExportMetadata("Description", "Lbl_SequenceContainer_ConditionalContainer_Description")]
    [ExportMetadata("Icon", "ConditionalInstructionSetSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]
    public partial class ConditionalContainer : SequenceContainer, IValidatable, ITrueFalse {

        [ImportingConstructor]
        public ConditionalContainer() : base(new ConditionalStrategy()) {
        }

        private ConditionalContainer(ConditionalContainer cloneMe) : this() {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                IsExpanded = cloneMe.IsExpanded;
                Items = new ObservableCollection<ISequenceItem>(cloneMe.Items.Select(i => i.Clone() as ISequenceItem));

                foreach (var item in Items) {
                    item.AttachNewParent(this);
                }
            }
        }

        [IsExpression]
        public partial double Predicate { get; set; }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            RefreshPredicateExpression();
        }

        public override bool Validate() {
            var valid = ValidateItems();
            var issues = new List<string>();

            RefreshPredicateExpression();

            if (string.IsNullOrWhiteSpace(PredicateExpression.Definition)) {
                issues.Add(Loc.Instance["Lbl_SequenceContainer_ConditionalContainer_ExpressionRequired"]);
            } else {
                Expression.ValidateExpressions(issues, PredicateExpression);
            }

            Issues = issues;
            RaisePropertyChanged(nameof(Issues));
            return valid && issues.Count == 0;
        }

        private bool ValidateItems() {
            var valid = true;

            foreach (var item in GetItemsSnapshot()) {
                if (item is IValidatable validatable) {
                    try {
                        valid = validatable.Validate() && valid;
                    } catch (System.Exception ex) {
                        Logger.Error(ex);
                        valid = false;
                    }
                }
            }

            return valid;
        }

        public override string ToString() {
            return $"Category: {Category}, Container: {nameof(ConditionalContainer)}, Predicate: {PredicateExpression.Definition}";
        }

        partial void AfterClone(ConditionalContainer clone) {
            clone.RefreshPredicateExpression();
        }

        private void RefreshPredicateExpression() {
            PredicateExpression.ForceAnnotated = !string.IsNullOrWhiteSpace(PredicateExpression.Definition);
            PredicateExpression.Evaluate(true);
        }
    }
}
