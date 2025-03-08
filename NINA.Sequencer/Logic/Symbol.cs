using Newtonsoft.Json;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using NINA.Sequencer.Container;
using System.Text;
using NINA.Core.Utility;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using System.Collections.Concurrent;
using NINA.Astrometry;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.SequenceItem.Expressions;

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
                if (copyMe.Expr != null) {
                    Expr = new Expression(copyMe.Expr.Definition, this);
                }
            }
        }

        static public SequenceContainer GlobalSymbols = new SequentialContainer() { Name = "Global Symbols" };

        public bool isDataSymbol { get; set; } = false;

        public static readonly String VALID_SYMBOL = "^[a-zA-Z][a-zA-Z0-9-+_]*$";

        public bool IsDuplicate { get; private set; } = false;

        public static void Warn(string str) {
            Logger.Warning(str);
        }

        protected ISequenceContainer LastSParent { get; set; }

        static private bool IsAttachedToRoot(ISequenceContainer container) {
            ISequenceEntity p = container;
            while (p != null) {
                if (p is SequenceRootContainer || (p == GlobalSymbols)) {
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
            if ((sym is DefineGlobalVariable || sym is DefineGlobalConstant) && !IsAttachedToRoot(sym.Parent)) {
                // This is an orphaned definition; allow it to be redefined
                dict[id] = this;
                return id;
            }
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
            // Make sure adler's problem sequence works here (fixed in Powerups)
            if (!IsAttachedToRoot(Parent) && (Parent != GlobalSymbols) && !(this is DefineGlobalVariable || this is DefineGlobalConstant)) {
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

            if (Expr != null) {
                Expr = new Expression(Expr?.Definition ?? "", Parent, this);
            }

            try {
                if (Identifier != null && Identifier.Length == 0) return;
                SymbolDictionary cached;
                if (SymbolCache.TryGetValue(sParent, out cached)) {
                    try {
                        if (Debugging) {
                            Logger.Info("APC: Added " + Identifier + " to " + sParent.Name);
                        }
                        bool added = cached.TryAdd(Identifier, this);

                        if (!added && sParent == GlobalSymbols) {
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

        private Expression _expr = null;

        [JsonProperty]
        public Expression Expr {
            get => _expr;
            set {
                _expr = value;
                _expr.SymbolBroker = SymbolBroker;
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

        public ISequenceContainer SParent() {
            if (Parent == null) {
                return null;
            } else if (this is DefineGlobalVariable || this is DefineGlobalConstant) {
                return GlobalSymbols;
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

        public static Symbol FindGlobalSymbol(string identifier) {
            SymbolDictionary cached;
            Symbol global = null;

            if (SymbolCache.TryGetValue(GlobalSymbols, out cached)) {

                // Prune orphaned global symbols
                foreach (var kvp in cached) {
                    Symbol sym = kvp.Value;
                    ISequenceEntity context = sym.Expr.Context;
                    if (context == null || !IsAttachedToRoot(context)) {
                        cached.TryRemove(kvp.Key, out _);
                    }
                }

                if (cached.ContainsKey(identifier)) {
                    global = cached[identifier];
                    // Don't find symbols that aren't part of the current sequence
                    if (!IsAttachedToRoot(global)) {
                        return null;
                    }
                }
            }
            if (global is DefineGlobalVariable || global is DefineGlobalConstant) return global;
            return null;
        }

        public static void ShowSymbols(object sender) {
            TextBox tb = (TextBox)sender;
            BindingExpression be = tb.GetBindingExpression(TextBox.TextProperty);
            Expression exp = be.ResolvedSource as Expression;
            ISymbolBrokerVM broker = exp.SymbolBroker;

            if (exp == null) {
                Symbol s = be.ResolvedSource as Symbol;
                if (s != null) {
                    exp = s.Expr;
                } else {
                    tb.ToolTip = "??";
                    return;
                }
            }

            if (exp.Definition?.Length == 0 && exp.Range != null) {
                tb.ToolTip = "Value must be between " + exp.Range[0] + " and " + exp.Range[1];
                return;
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
                    sb.Append(") = ");
                    sb.Append(sym.Expr.Error != null ? sym.Expr.Error : sym.Expr.ValueString);
                } else {
                    // We're a data value
                    SymbolBrokerVM.DataSource ds = broker.GetDataSource(kvp.Key);
                    // Get the source of the data, and the data itself
                    if (ds != null) {
                        sb.Append(" (" + ds.source + ") = ");
                        if (ds.data is double d) {
                            sb.Append(Math.Round(d, 3));

                        } else {
                            sb.Append(ds.data);
                        }
                    } else {
                        sb.Append( "??");
                    }
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
            try {
                return $"Symbol: Identifier {Identifier}, in {SParent()?.Name} with value {Expr.Value}";
            } catch (Exception ex) {
                Logger.Error("Foo");
                return "Foo";
            }
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
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_') {
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
 
        public static IList<string> GetSwitches() {
            lock (SYMBOL_LOCK) {
                return Switches;
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
