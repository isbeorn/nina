using Newtonsoft.Json;
using System.ComponentModel.Composition;
using NINA.Sequencer.Container;

namespace NINA.Sequencer.SequenceItem.Expressions {
    [ExportMetadata("Name", "Define Variable")]
    [ExportMetadata("Description", "Creates a Global Variable whose value can be used in Expressions")]
    [ExportMetadata("Icon", "VariableSVG")]
    [ExportMetadata("Category", "Expressions")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class DefineGlobalVariable : DefineVariable {

        [ImportingConstructor]
        public DefineGlobalVariable() : base() {
        }

        public DefineGlobalVariable(DefineGlobalVariable copyMe) : base(copyMe) {
        }

        public DefineGlobalVariable(string id, string def, ISequenceContainer parent) : base(id, def, parent) {
        }

        public override object Clone() {
            DefineGlobalVariable clone = new DefineGlobalVariable(this);
            PreClone(clone);
            return clone;
        }

        public override string ToString() {
            if (Expr != null) {
                return $"Global Variable: {Identifier}, Definition: {Expr.Definition}, Parent: {Parent?.Name}, Expr: {Expr}";

            } else {
                return $"Global Variable: {Identifier}, Parent: {Parent?.Name} Expr: null";
            }
        }
    }
}
