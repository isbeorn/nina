using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System.ComponentModel.Composition;
using System.Threading;
using System.Text.RegularExpressions;
using NINA.Sequencer.Validations;
using System.Reflection;
using NINA.Sequencer.Logic;
using System.Drawing;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Sequencer.Generators;

namespace NINA.Sequencer.SequenceItem.Expressions {
    [ExportMetadata("Name", "Define Constant")]
    [ExportMetadata("Description", "Creates a Constant whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "ConstantSVG")]
    [ExportMetadata("Category", "Expressions")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public partial class DefineConstant : Symbol, IValidatable {

        [ImportingConstructor]
        public DefineConstant() : base() {
            Name = Name;
            Icon = Icon;
        }
        public DefineConstant(DefineConstant copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
            }
        }

        public override object Clone() {
            DefineConstant clone = new DefineConstant(this);

            clone.Identifier = Identifier;
            if (Expr != null) {
                clone.Expr = new Expression(Expr.Definition, clone.Parent, this);
            } else {
                clone.Expr = new Expression("", clone.Parent, this);
            }
            return clone;
        }

        public override string ToString() {
            try {
                if (Expr != null) {
                    return $"Define Constant: {Identifier}, Definition: {Expr.Definition}, Parent: {Parent?.Name} Expr: {Expr}";
                } else {
                    return $"Define Constant: {Identifier}, Parent: {Parent?.Name} Expr: null";
                }
            } catch (Exception ex) {
                return "Foo";
            }
        }

        public override bool Validate() {
            if (!IsAttachedToRoot()) return true;

            IList<string> i = new List<string>();

            if (Identifier.Length == 0 || Expr.Definition.Length == 0) {
                i.Add("A name and a value must be specified");
            } else if (!Regex.IsMatch(Identifier, VALID_SYMBOL)) {
                i.Add("The name of a Constant must be alphanumeric");
            } else if (IsDuplicate) {
                i.Add("The Constant is already defined here; this definition will be ignored.");
            }

            Expression.AddExprIssues(Issues, Expr);

            foreach (var kvp in Expr.Resolved) {
                if (kvp.Value is DefineVariable) {
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

        // Global Constants

        private string globalName = null;
        public string GlobalName {
            get => globalName;
            set {
                globalName = value;
            }
        }

        public void SetGlobalName(string name) {
            //PropertyInfo pi = WhenPluginObject.GetType().GetProperty(GlobalName);
            //pi?.SetValue(WhenPluginObject, name, null);
        }

        private string globalValue = null;
        public string GlobalValue {
            get => globalValue;
            set {
                globalValue = value;
            }
        }

        public void SetGlobalValue(string expr) {
            //PropertyInfo pi = WhenPluginObject.GetType().GetProperty(GlobalValue);
            //pi?.SetValue(WhenPluginObject, expr, null);
        }

        public string GlobalAll { get; set; }

        public string Dummy;

        private bool allProfiles = true;

        public bool AllProfiles {
            get => allProfiles;
            set {
                if (GlobalName != null) {
                    //PropertyInfo pi = WhenPluginObject.GetType().GetProperty(GlobalAll);
                    //pi?.SetValue(WhenPluginObject, value, null);
                }
                allProfiles = value;
            }
        }
    }
}
