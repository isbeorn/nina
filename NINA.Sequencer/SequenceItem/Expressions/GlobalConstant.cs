using Newtonsoft.Json;
using System.ComponentModel.Composition;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.Expressions {
    [ExportMetadata("Name", "Define Constant")]
    [ExportMetadata("Description", "Creates a Global Constant whose value can be used in Expressions")]
    [ExportMetadata("Icon", "VariableSVG")]
    [ExportMetadata("Category", "Expressions")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class GlobalConstant : Constant {

        [ImportingConstructor]
        public GlobalConstant() : base() {
            Name = Name;
            Icon = Icon;
        }

        public GlobalConstant(GlobalConstant copyMe) : base() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
            }
        }

        public override object Clone() {
            GlobalConstant clone = new GlobalConstant(this);
            clone.Identifier = Identifier;
            clone.Expr = new Expression(Expr != null ? Expr.Definition : "", clone.Parent, this);
            return clone;
        }

        public override string ToString() {
            if (Expr != null) {
                return $"Global Constant: {Identifier}, Definition: {Expr.Definition}, Parent: {Parent?.Name}, Expr: {Expr}";
            } else {
                return $"Global Constant: {Identifier}, Parent: {Parent?.Name} Expr: null";
            }
        }
    }
}
