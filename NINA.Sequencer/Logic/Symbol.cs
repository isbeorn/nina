using Newtonsoft.Json;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using NINA.Sequencer.Container;
using System.Text;
using NINA.Core.Utility;
using NINA.Sequencer;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Equipment.Equipment.MyFilterWheel;
using System.IO;
using System.Linq;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyTelescope;
using System.Collections.Concurrent;
using NINA.Astrometry;
using NINA.Equipment.Equipment.MyGuider.PHD2.PhdEvents;
using NINA.Equipment.Equipment.MyGuider.PHD2;
using Newtonsoft.Json.Linq;
using NINA.Core.Utility.Notification;
using Google.Protobuf;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Mediator;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.Logic {

    [JsonObject(MemberSerialization.OptIn)]

    public abstract class Symbol : SequenceItem.SequenceItem, IValidatable {

        public class SymbolDictionary : ConcurrentDictionary<string, Symbol> { public static explicit operator ConcurrentDictionary<object, object>(SymbolDictionary v) { throw new NotImplementedException(); } };

        public static ConcurrentDictionary<ISequenceContainer, SymbolDictionary> SymbolCache = new ConcurrentDictionary<ISequenceContainer, SymbolDictionary>();

        public static ConcurrentDictionary<Symbol, List<string>> Orphans = new ConcurrentDictionary<Symbol, List<string>>();

        [ImportingConstructor]
        public Symbol() {
            Name = Name;
            Icon = Icon;
        }

        public Symbol(Symbol copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
                Identifier = copyMe.Identifier;
                Definition = copyMe.Definition;
            }
        }

        static public SequenceContainer GlobalContainer = new SequentialContainer() { Name = "Global Constants" };

        static public SequenceContainer GlobalVariables = new SequentialContainer() { Name = "Global Variables" };

        public bool IsGlobalVariable { get; set; } = false;

        public bool isDataSymbol { get; set; } = false;

        public class Keys : Dictionary<string, object>;

        public static readonly String VALID_SYMBOL = "^[a-zA-Z][a-zA-Z0-9-+_]*$";

        public bool IsDuplicate { get; private set; } = false;

        public static void Warn(string str) {
            Logger.Warning(str);
        }

        protected ISequenceContainer LastSParent { get; set; }

        static private bool IsAttachedToRoot(ISequenceContainer container) {
            ISequenceEntity p = container;
            while (p != null) {
                if (p is SequenceRootContainer) { // || p == WhenPluginObject.Globals) {
                    return true;
                } else {
                    p = p.Parent;
                }
            }
            return false;
        }

        static public bool IsAttachedToRoot(ISequenceEntity item) {
            if (item.Parent == null) return false;
            return IsAttachedToRoot(item.Parent);
        }

        // Must prevent cycles
        public static void SymbolDirty(Symbol sym) {
            if (Debugging) {
                Logger.Info("SymbolDirty: " + sym);
            }
            List<Symbol> dirtyList = new List<Symbol>();
            iSymbolDirty(sym, dirtyList);
        }

        public static void iSymbolDirty(Symbol sym, List<Symbol> dirtyList) {
            Debug.WriteLine("SymbolDirty: " + sym);
            dirtyList.Add(sym);
            // Mark everything in the chain dirty
            foreach (var consumer in sym.Consumers) {
                Expression expr = consumer.Key;
                expr.ReferenceRemoved(sym);
                Symbol consumerSym = expr.Symbol;
                if (!expr.Dirty && consumerSym != null) {
                    if (!dirtyList.Contains(consumerSym)) {
                        iSymbolDirty(consumerSym, dirtyList);
                    }
                }
                expr.Dirty = true;
                //expr.Evaluate();
            }
        }

        private string GenId(SymbolDictionary dict, string id) {

            Symbol sym;
            _ = dict.TryGetValue(id, out sym);
            //if (sym is SetGlobalVariable && !IsAttachedToRoot(sym.Parent)) {
            //    // This is an orphaned definition; allow it to be redefined
            //    dict[id] = this;
            //    return id;
            //}
            Notification.ShowWarning("The Constant/Variable " + id + " is already defined");
            return "";
        }

        private ISequenceContainer LastParent;

        public override void AfterParentChanged() {
            base.AfterParentChanged();

            if (Parent == null) {
                Logger.Info("Null");
            }

            ISequenceContainer sParent = SParent();
            if (sParent == LastSParent) {
                return;
            }
            Debug.WriteLine("APC: " + this + ", New Parent = " + ((sParent == null) ? "null" : sParent.Name));
            if (!IsAttachedToRoot(Parent)) { // && (Parent != WhenPluginObject.Globals) && !(this is SetGlobalVariable)) {
                if (Expr != null) {
                    // Clear out orphans of this Symbol
                    Orphans.TryRemove(this, out _);
                    // We've deleted this Symbol
                    SymbolDictionary cached;
                    if (LastSParent == null) {
                        Warn("Removed symbol " + this + " has no LastSParent?");
                        // We're saving a template?
                        return;
                    }
                    if (SymbolCache.TryGetValue(LastSParent, out cached)) {
                        if (cached.TryRemove(Identifier, out _)) {
                            SymbolDirty(this);
                        } else {
                            Warn("Deleting " + this + " but not in SParent's cache?");
                        }
                    } else {
                        Warn("Deleting " + this + " but SParent has no cache?");
                    }
                }
                return;
            }
            LastSParent = sParent;

            Expr = new Expression(Definition, Parent, this);

            try {
                if (Identifier != null && Identifier.Length == 0) return;
                SymbolDictionary cached;
                if (SymbolCache.TryGetValue(sParent, out cached)) {
                    try {
                        if (Debugging) {
                            Logger.Info("APC: Added " + Identifier + " to " + sParent.Name);
                        }
                        bool added = cached.TryAdd(Identifier, this);

                        if (!added && sParent == GlobalVariables) {
                            Symbol gv;
                            cached.TryGetValue(Identifier, out gv);
                            if (gv != null) {
                                Logger.Warning("New Symbol for Global Variable: " + Identifier);
                                SymbolDirty(gv);
                                gv.Consumers.Clear();
                                cached.TryUpdate(Identifier, this, gv);
                            }
                        } else if (!added) {
                            Identifier = GenId(cached, Identifier);
                            return;
                        }
                    } catch (ArgumentException) {
                        if (true) { // ***** (sParent != WhenPluginObject.Globals) {
                            IsDuplicate = true;
                            Identifier = GenId(cached, Identifier);
                            cached.TryAdd(Identifier, this);
                        }
                    }
                } else {
                    SymbolDictionary newSymbols = new SymbolDictionary();
                    newSymbols.TryAdd(Identifier, this);
                    SymbolCache.TryAdd(sParent, newSymbols);
                    if (Debugging) {
                        Logger.Info("APC: Added " + sParent.Name + " to SymbolCache");
                        Logger.Info("APC: Added " + Identifier + " to " + sParent.Name);
                    }

                    foreach (var consumer in Consumers) {
                        consumer.Key.RemoveParameter(Identifier);
                    }

                    // Can we see if the Parent moves?
                    // Parent.AfterParentChanged += ??
                }
            } catch (Exception ex) {
                Logger.Error("Exception in Symbol evaluation: " + ex.Message);
            }

            LastParent = Parent;
        }

        protected static bool Debugging = false;

        private string _identifier = "";

        [JsonProperty]
        public string Identifier {
            get => _identifier;
            set {
                if (Parent == null) {
                    _identifier = value;
                    return;
                }

                ISequenceContainer sParent = SParent();

                SymbolDictionary cached = null;
                if (value == _identifier) {
                    return;
                } else if (_identifier.Length != 0) {
                    // If there was an old value, remove it from Parent's dictionary
                    if (!IsDuplicate && SymbolCache.TryGetValue(sParent, out cached)) {
                        if (Debugging) {
                            Logger.Info("Removing " + value + " from " + sParent.Name);
                        }
                        cached.TryRemove(value, out _);
                        SymbolDirty(this);
                    }
                }

                _identifier = value;

                if (value.Length == 0) return;

                // Store the symbol in the SymbolCache for this Parent
                if (Parent != null) {
                    if (cached != null || SymbolCache.TryGetValue(sParent, out cached)) {
                        try {
                            if (!cached.TryAdd(Identifier, this)) {
                                _identifier = GenId(cached, Identifier);
                            }
                            if (Debugging) {
                                Logger.Info("Adding " + Identifier + " to " + sParent.Name);
                            }
                        } catch (ArgumentException) {
                            Logger.Warning("Attempt to add duplicate Symbol at same level in sequence: " + Identifier);
                        }
                    } else {
                        SymbolDictionary newSymbols = new SymbolDictionary();
                        if (Debugging) {
                            Logger.Info("Creating new SymbolCache entry for " + this.Name);
                        }
                        SymbolCache.TryAdd(sParent, newSymbols);
                        newSymbols.TryAdd(Identifier, this);
                    }
                }

                //if (this is SetConstant constant && constant.GlobalName != null) {
                //    constant.SetGlobalName(Identifier);
                //}
            }
        }

        private string _definition = "";

        [JsonProperty]
        public string Definition {
            get => _definition;
            set {
                if (value == _definition) {
                    if (Expr != null && value != Expr.Definition) {
                        Logger.Warning("Definition not reflected in Expression; user changed value manually");
                    } else {
                        return;
                    }
                }
                _definition = value;
                if (SParent() != null) {
                    if (Expr != null) {
                        if (Debugging) {
                            Logger.Info("Setting Definition for " + Identifier + " in " + SParent().Name + ": " + value);
                        }
                        Expr.Definition = value;
                    }
                }
                RaisePropertyChanged("Expr");

                //if (this is SetConstant constant && constant.GlobalValue != null) {
                //    constant.SetGlobalValue(value);
                //}

            }
        }

        private Expression _expr = null;
        public Expression Expr {
            get => _expr;
            set {
                _expr = value;
                RaisePropertyChanged();
            }
        }

        public IList<string> Issues { get; set; }

        public bool IsReference { get; set; } = false;

        protected bool IsAttachedToRoot() {
            ISequenceContainer p = Parent;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return true;
                }
                p = p.Parent;
            }
            return false;
        }

        public ConcurrentDictionary<Expression, byte> Consumers = new ConcurrentDictionary<Expression, byte>();
        //public static WhenPlugin WhenPluginObject { get; set; }

        public ISequenceContainer SParent() {
            if (Parent == null) {
                return null;
                //} else if (this is SetGlobalVariable) {
                //    return GlobalVariables;
                //} else if (Parent is CVContainer cvc) {
                //    if (cvc.Parent is TemplateContainer tc) {
                //        return tc.Parent;
                //    } else {
                //        return cvc.Parent;
                //    }
            } else {
                return Parent;
            }
        }


        public void AddConsumer(Expression expr) {
            if (!Consumers.ContainsKey(expr)) {
                Consumers.TryAdd(expr, 0);
            }
        }

        public void RemoveConsumer(Expression expr) {
            if (!Consumers.TryRemove(expr, out _)) {
                Warn("RemoveConsumer: " + expr + " not found in " + this);
            }
        }

        public static Symbol FindSymbol(string identifier, ISequenceContainer context) {
            while (context != null) {
                SymbolDictionary cached;
                if (SymbolCache.TryGetValue(context, out cached)) {
                    if (cached.ContainsKey(identifier)) {
                        if (Debugging) {
                            Logger.Info("FindSymbol '" + identifier + "' returning " + cached[identifier]);
                        }
                        return cached[identifier];
                    }
                }
                context = context.Parent;
            }
            return FindGlobalSymbol(identifier);
        }

        public static void DumpSymbols() {
            foreach (var c in SymbolCache) {
                Logger.Info("\r\nIn SymbolCache for " + c.Key.Name);
                foreach (var d in c.Value) {
                    Logger.Info("  -- " + d.Key + " / " + d.Value.ToString());
                }
            }
            foreach (var x in Symbol.GetSwitchWeatherKeys()) {
                Logger.Info(x.Key + ": " + x.Value.ToString());
            }
        }

        public static Symbol FindGlobalSymbol(string identifier) {
            SymbolDictionary cached;
            Symbol global = null;
            if (SymbolCache.TryGetValue(GlobalVariables, out cached)) {
                if (cached.ContainsKey(identifier)) {
                    global = cached[identifier];
                    // Don't find symbols that aren't part of the current sequence
                    if (!IsAttachedToRoot(global)) {
                        return null;
                    }
                }
            }
            //if (global is SetGlobalVariable) return global;
            return null;
        }

        public static void ShowSymbols(object sender) {
            TextBox tb = (TextBox)sender;
            BindingExpression be = tb.GetBindingExpression(TextBox.TextProperty);
            Expression exp = be.ResolvedSource as Expression;
            Dictionary<string, object> DataSymbols = Symbol.GetSwitchWeatherKeys();

            if (exp == null) {
                Symbol s = be.ResolvedSource as Symbol;
                if (s != null) {
                    exp = s.Expr;
                } else {
                    tb.ToolTip = "??";
                    return;
                }
            }

            Dictionary<string, Symbol> syms = exp.Resolved;
            int cnt = syms.Count;
            if (cnt == 0) {
                if (exp.References.Count == 1) {
                    tb.ToolTip = "The symbol is not yet defined";
                } else {
                    tb.ToolTip = "No defined symbols used in this expression";
                }
                return;
            }
            StringBuilder sb = new StringBuilder(cnt == 1 ? "Symbol: " : "Symbols: ");

            foreach (var kvp in syms) {
                Symbol sym = kvp.Value as Symbol;
                sb.Append(kvp.Key.ToString());
                if (sym != null) {
                    sb.Append(" (in ");
                    sb.Append(sym.SParent().Name);
                    ISequenceContainer sParent = sym.SParent();
                    if (sParent != sym.Parent) {
                        //if (sym.Parent is CVContainer) {
                        //    sb.Append("/" + sym.Parent.Name);
                        //    if (sym.Parent.Parent is TemplateContainer tc) {
                        //        sb.Append("/TBR");
                        //        if (tc.PseudoParent != null && tc.PseudoParent is TemplateByReference tbr) {
                        //            sb.Append("-" + tbr.TemplateName);
                        //        }
                        //    }
                        //} else if (sParent != GlobalVariables) {
                        //    sb.Append(" - WTF");
                        //}
                    }
                    sb.Append(") = ");
                    sb.Append(sym.Expr.Error != null ? sym.Expr.Error : sym.Expr.ValueString);
                } else {
                    // We're a data value
                    sb.Append(" (Data) = ");
                    sb.Append(DataSymbols.GetValueOrDefault(kvp.Key, "??"));
                }
                if (--cnt > 0) sb.Append("; ");
            }

            tb.ToolTip = sb.ToString();
        }
        public static string ShowSymbols(Expression exp) {
            //Dictionary<string, object> DataSymbols = Symbol.GetSwitchWeatherKeys();

            if (exp == null) {
                return "??";
            }

            Dictionary<string, Symbol> syms = exp.Resolved;
            int cnt = syms.Count;
            if (cnt == 0) {
                if (exp.References.Count == 1) {
                    return "The symbol is not yet defined\r\n";
                } else {
                    return "No defined symbols used in this expression\r\n";
                }
            }
            StringBuilder sb = new StringBuilder(cnt == 1 ? "Symbol: " : "Symbols: ");

            foreach (var kvp in syms) {
                Symbol sym = kvp.Value as Symbol;
                sb.Append(kvp.Key.ToString());
                if (sym != null) {
                    sb.Append(" (in ");
                    sb.Append(sym.SParent().Name);
                    ISequenceContainer sParent = sym.SParent();
                    if (sParent != sym.Parent) {
                        //if (sym.Parent is CVContainer) {
                        //    sb.Append("/" + sym.Parent.Name);
                        //    if (sym.Parent.Parent is TemplateContainer tc) {
                        //        sb.Append("/TBR");
                        //        if (tc.PseudoParent != null && tc.PseudoParent is TemplateByReference tbr) {
                        //            sb.Append("-" + tbr.TemplateName);
                        //        }
                        //    }
                        //} else {
                        sb.Append(" - WTF");
                        //}
                    }
                    sb.Append(") = ");
                    sb.Append(sym.Expr.Error != null ? sym.Expr.Error : sym.Expr.Value.ToString());
                } else {
                    // We're a data value
                    sb.Append(" (Data) = ");
                    //sb.Append(DataSymbols.GetValueOrDefault(kvp.Key, "??"));
                }
                if (--cnt > 0) sb.Append("; ");
            }

            sb.Append("\r\n");
            return sb.ToString();
        }


        public abstract bool Validate();

        public override string ToString() {
            return $"Symbol: Identifier {Identifier}, in {SParent()?.Name} with value {Expr.Value}";
        }


        // DATA SYMBOLS


        private static string[] WeatherData = new string[] { "CloudCover", "DewPoint", "Humidity", "Pressure", "RainRate", "SkyBrightness", "SkyQuality", "SkyTemperature",
            "StarFWHM", "Temperature", "WindDirection", "WindGust", "WindSpeed"};

        public static string RemoveSpecialCharacters(string str) {
            if (str == null) {
                return "__Null__";
            }
            StringBuilder sb = new StringBuilder();
            foreach (char c in str) {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_') {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }


        private static ISwitchMediator SwitchMediator { get; set; }
        private static IWeatherDataMediator WeatherDataMediator { get; set; }
        private static ICameraMediator CameraMediator { get; set; }
        private static IDomeMediator DomeMediator { get; set; }
        private static IFlatDeviceMediator FlatMediator { get; set; }
        private static IFilterWheelMediator FilterWheelMediator { get; set; }
        private static IProfileService ProfileService { get; set; }
        private static IRotatorMediator RotatorMediator { get; set; }
        private static ISafetyMonitorMediator SafetyMonitorMediator { get; set; }
        private static IFocuserMediator FocuserMediator { get; set; }
        private static ITelescopeMediator TelescopeMediator { get; set; }
        //private static IMessageBroker MessageBroker { get; set; }
        private static IGuiderMediator GuiderMediator { get; set; }


        private static ConditionWatchdog ConditionWatchdog { get; set; }
        private static IList<string> Switches { get; set; } = new List<string>();

        public class Array : Dictionary<object, object>;
        public static Dictionary<string, Array> Arrays { get; set; } = new Dictionary<string, Array>();


        public class VariableMessage {
            public object value;
            public DateTimeOffset? expiration;

            public VariableMessage(object value, DateTimeOffset? expiration) {
                this.value = value;
                this.expiration = expiration;
            }
        }
 
        public static Symbol.Keys MessageKeys = new Symbol.Keys();

        public static Symbol.Keys SwitchWeatherKeys { get; set; } = new Symbol.Keys();

        public static Symbol.Keys GetSwitchWeatherKeys() {
            lock (SYMBOL_LOCK) {
                return SwitchWeatherKeys;
            }
        }

        public static IList<string> GetSwitches() {
            lock (SYMBOL_LOCK) {
                return Switches;
            }
        }

        public static Symbol.SymbolDictionary DataSymbols { get; set; } = new Symbol.SymbolDictionary();

        public ConcurrentDictionary<string, Symbol> GetDataSymbols() {
            lock (SYMBOL_LOCK) {
                return DataSymbols;
            }
        }

        private static ObserverInfo Observer = null;

        public static Object SYMBOL_LOCK = new object();

        private static HashSet<string> LoggedOnce = new HashSet<string>();
        public static void LogOnce(string message) {
            if (LoggedOnce.Contains(message)) return;
            Logger.Warning(message);
            LoggedOnce.Add(message);
        }

 
        public static Task UpdateSwitchWeatherData() {
            return Task.CompletedTask;
        }
    }
}
