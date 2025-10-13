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
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using NINA.Sequencer.Container;
using System.Text;
using NINA.Core.Utility;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Concurrent;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.SequenceItem.Expressions;

namespace NINA.Sequencer.Logic {

    [JsonObject(MemberSerialization.OptIn)]

    public abstract class UserSymbol : SequenceItem.SequenceItem, IValidatable {

        public class SymbolDictionary : ConcurrentDictionary<string, UserSymbol> { public static explicit operator ConcurrentDictionary<object, object>(SymbolDictionary v) { throw new NotImplementedException(); } };

        public static ConcurrentDictionary<ISequenceContainer, SymbolDictionary> SymbolCache = new ConcurrentDictionary<ISequenceContainer, SymbolDictionary>();

        public static ConcurrentDictionary<UserSymbol, List<string>> Orphans = new ConcurrentDictionary<UserSymbol, List<string>>();

        [ImportingConstructor]
        public UserSymbol() {
            Name = Name;
            Icon = Icon;
        }

        public UserSymbol(UserSymbol copyMe) : this() {
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

        public static void SymbolDirty(UserSymbol sym) {
            if (Debugging) {
                Logger.Info("SymbolDirty: " + sym);
            }
            // Prevent cycles
            List<UserSymbol> dirtyList = new List<UserSymbol>();
            iSymbolDirty(sym, dirtyList);
        }

        public static void iSymbolDirty(UserSymbol sym, List<UserSymbol> dirtyList) {
            Debug.WriteLine("SymbolDirty: " + sym);
            dirtyList.Add(sym);
            // Mark everything in the chain dirty
            foreach (var consumer in sym.Consumers) {
                Expression expr = consumer.Key;
                expr.ReferenceRemoved(sym);
                UserSymbol consumerSym = expr.Symbol;
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

            UserSymbol sym;
            _ = dict.TryGetValue(id, out sym);
            if ((sym is GlobalVariable || sym is GlobalConstant) && !IsAttachedToRoot(sym.Parent)) {
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
            if (!IsAttachedToRoot(Parent) && (Parent != GlobalSymbols) && !(this is GlobalVariable || this is GlobalConstant)) {
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

                        Logger.Info("Entries for " + sParent.Name + ": " + cached.Count);

                        if (!added && sParent == GlobalSymbols) {
                            UserSymbol gv;
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
                        IsDuplicate = true;
                        Identifier = GenId(cached, Identifier);
                        cached.TryAdd(Identifier, this);
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
                }
            } catch (Exception ex) {
                Logger.Error("Exception in Symbol evaluation: " + ex.Message);
            }

            LastParent = Parent;
        }

        protected static bool Debugging = true;

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
            } else if (this is GlobalVariable || this is GlobalConstant) {
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

        public static UserSymbol FindSymbol(string identifier, ISequenceContainer context) {
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

        public static UserSymbol FindGlobalSymbol(string identifier) {
            SymbolDictionary cached;
            UserSymbol global = null;

            if (SymbolCache.TryGetValue(GlobalSymbols, out cached)) {

                // Prune orphaned global symbols
                foreach (var kvp in cached) {
                    UserSymbol sym = kvp.Value;
                    ISequenceEntity context = sym.Expr.Context;
                    if (context == null || !IsAttachedToRoot(context)) {
                        cached.TryRemove(kvp.Key, out _);
                        Logger.Info("Removing " + kvp.Key + " from GlobalSymbols");
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
            if (global is GlobalVariable || global is GlobalConstant) return global;
            return null;
        }

        public static void ShowSymbols(object sender) {
            TextBox tb = (TextBox)sender;
            BindingExpression be = tb.GetBindingExpression(TextBox.TextProperty);
            Expression exp = be.ResolvedSource as Expression;
            ISymbolBroker broker = exp.SymbolBroker;

            if (exp == null) {
                UserSymbol s = be.ResolvedSource as UserSymbol;
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

            IReadOnlyDictionary<string, UserSymbol> syms = exp.Resolved;
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
                UserSymbol sym = kvp.Value as UserSymbol;
                sb.Append(kvp.Key.ToString());
                if (sym != null) {
                    sb.Append(" (in ");
                    sb.Append(sym.SParent().Name);
                    ISequenceContainer sParent = sym.SParent();
                    sb.Append(") = ");
                    sb.Append(sym.Expr.Error != null ? sym.Expr.Error : sym.Expr.ValueString);
                } else {
                    // We're a data value
                    Symbol val;
                    broker.TryGetSymbol(kvp.Key, out val);
                    // Get the source of the data, and the data itself
                    if (val is Symbol ds) {
                        sb.Append(" (" + ds.Category + ") = ");
                        if (ds.Value is double d) {
                            sb.Append(Math.Round(d, 3));

                        } else {
                            sb.Append(ds.Value);
                        }
                    } else {
                        sb.Append( "??");
                    }
                }
                if (--cnt > 0) sb.Append("; ");
            }

            tb.ToolTip = sb.ToString();
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

        private static HashSet<string> LoggedOnce = new HashSet<string>();
        public static void LogOnce(string message) {
            if (LoggedOnce.Contains(message)) return;
            Logger.Warning(message);
            LoggedOnce.Add(message);
        }
    }
}
