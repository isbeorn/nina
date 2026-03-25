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
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using NINA.Sequencer.Container;
using System.Text;
using System.Text.RegularExpressions;
using NINA.Core.Utility;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Concurrent;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Core.Locale;

namespace NINA.Sequencer.Logic {

    [JsonObject(MemberSerialization.OptIn)]

    public abstract class UserSymbol : SequenceItem.SequenceItem, IValidatable {

        private static HashSet<string> LoggedOnce = new HashSet<string>();

        private Expression _expr = null;

        private string _identifier = "";

        protected static bool Debugging = false;

        public static readonly string VALID_SYMBOL = "^[a-zA-Z_][a-zA-Z0-9_]*$";

        /// <summary>
        /// Precompiled regex for validating symbol identifiers. Use this instead of Regex.IsMatch(str, VALID_SYMBOL) for better performance.
        /// </summary>
        public static readonly Regex ValidSymbolRegex = new Regex(VALID_SYMBOL, RegexOptions.Compiled);

        static public SequenceContainer GlobalSymbols { get; } = new SequentialContainer() { Name = "Global Symbols" };

        public static ConcurrentDictionary<ISequenceContainer, SymbolDictionary> SymbolCache { get; } = new ConcurrentDictionary<ISequenceContainer, SymbolDictionary>();

        public ConcurrentDictionary<Expression, byte>  Consumers { get; } = new ConcurrentDictionary<Expression, byte>();

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

        protected ISequenceContainer LastSParent { get; set; }

        [JsonProperty]
        public Expression Expr {
            get => _expr;
            set {
                _expr = value;
                _expr.SymbolBroker = SymbolBroker;
                RaisePropertyChanged();
            }
        }

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
                        if (!cached.TryRemove(_identifier, out _)) {
                            Logger.Warning("Could not remove " + value + " from " + sParent.Name);
                        }
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

        public bool IsDuplicate { get; private set; } = false;

        public IList<string> Issues { get; set; }

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

        private string GenId(SymbolDictionary dict, string id) {

            UserSymbol sym;
            _ = dict.TryGetValue(id, out sym);
            if ((sym is GlobalVariable || sym is GlobalConstant) && !IsAttachedToRoot(sym.Parent)) {
                // This is an orphaned definition; allow it to be redefined
                dict[id] = this;
                return id;
            }
            Notification.ShowWarning(Loc.Instance["LblConstantVariable"] + " " + id + " " + Loc.Instance["LblAlreadyDefined"], TimeSpan.FromSeconds(5));
            return "";
        }

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

        public static void ClearUserSymbols() {
            SymbolDictionary cached;
            if (SymbolCache.TryGetValue(GlobalSymbols, out cached)) {
                Logger.Info("Cleared UserSymbols");
                cached.Clear();
            }
        }

        public static UserSymbol FindGlobalSymbol(string identifier) {
            SymbolDictionary cached;
            UserSymbol global = null;

            if (SymbolCache.TryGetValue(GlobalSymbols, out cached)) {
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

        static public bool IsAttachedToRoot(ISequenceEntity item) {
            if (item.Parent == null) return false;
            return IsAttachedToRoot(item.Parent);
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

        public static void LogOnce(string message) {
            if (LoggedOnce.Contains(message)) return;
            Logger.Warning(message);
            LoggedOnce.Add(message);
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
                //tb.ToolTip = string.Format(Loc.Instance["LblValueBetween"], exp.Range[0], exp.Range[1]);
                tb.ToolTip = exp.RangeString(null);
                return;
            }

            IReadOnlyDictionary<string, UserSymbol> syms = exp.Resolved;
            int cnt = syms.Count;
            if (cnt == 0) {
                if (exp.References.Count == 1) {
                    tb.ToolTip = Loc.Instance["LblNotYetDefined"];
                } else {
                    tb.ToolTip = Loc.Instance["LblNoSymbols"];
                }
                return;
            }
            StringBuilder sb = new StringBuilder();

            foreach (var kvp in syms) {
                UserSymbol sym = kvp.Value as UserSymbol;
                sb.Append(kvp.Key.ToString());
                if (sym != null) {
                    sb.Append(" (");
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
                        sb.Append("??");
                    }
                }
                if (--cnt > 0) sb.Append("; ");
            }

            tb.ToolTip = sb.ToString();
        }

        public static void SymbolDirty(UserSymbol sym) {
            if (Debugging) {
                Logger.Info("SymbolDirty: " + sym);
            }

            if (sym == null || String.IsNullOrEmpty(sym.Identifier)) return;

            // Prevent cycles
            List<UserSymbol> dirtyList = new List<UserSymbol>();
            iSymbolDirty(sym, dirtyList);
        }

        public static void Warn(string str) {
            Logger.Warning(str);
        }

        public void AddConsumer(Expression expr) {
            if (!Consumers.ContainsKey(expr)) {
                Consumers.TryAdd(expr, 0);
            }
        }

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
                    // We've deleted this Symbol
                    SymbolDictionary cached;
                    if (LastSParent == null) {
                        if (Debugging) {
                            Warn("Removed symbol " + this + " has no LastSParent?");
                        }
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

                        if (Debugging) {
                            Logger.Info("Entries for " + sParent.Name + ": " + cached.Count);
                        }

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
        }

        public void RemoveConsumer(Expression expr) {
            if (!Consumers.TryRemove(expr, out _)) {
                Warn("RemoveConsumer: " + expr + " not found in " + this);
            }
        }

        public ISequenceContainer SParent() {
            if (Parent == null) {
                return null;
            } else if (this is GlobalVariable || this is GlobalConstant) {
                return GlobalSymbols;
            } else {
                return Parent;
            }
        }

        public override string ToString() {
            try {
                return $"Symbol: Identifier {Identifier}, in {SParent()?.Name} with value {Expr.Value}";
            } catch (Exception ex) {
                Logger.Error("Exception: " + ex.Message);
                return "{Exception in UserSymbol}";
            }
        }

        public abstract bool Validate();

        public class SymbolDictionary : ConcurrentDictionary<string, UserSymbol> { public static explicit operator ConcurrentDictionary<object, object>(SymbolDictionary v) { throw new NotImplementedException(); } };
    }
}
