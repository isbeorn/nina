using Newtonsoft.Json;
using System.ComponentModel.Composition;
using NINA.Sequencer.Container;

namespace NINA.Sequencer.SequenceItem.Expressions {
    [ExportMetadata("Name", "Lbl_SequenceItem_Symbols_DefineVariable_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Symbols_DefineVariable_Description")]
    [ExportMetadata("Icon", "VariableSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Symbol")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class GlobalVariable : Variable {

        [ImportingConstructor]
        public GlobalVariable() : base() {
        }

        public GlobalVariable(GlobalVariable copyMe) : base(copyMe) {
        }

        public GlobalVariable(string id, string def, ISequenceContainer parent) : base(id, def, parent) {
        }

        public override object Clone() {
            GlobalVariable clone = new GlobalVariable(this);
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
