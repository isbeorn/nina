using NCalc;
using NCalc.Exceptions;
using NCalc.Handlers;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Expressions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Media;
using static NINA.Sequencer.Logic.UserSymbol;

namespace NINA.Sequencer.Logic {
    [JsonObject(MemberSerialization.OptIn)]
    public class Expression : BaseINPC {

        private static readonly int ONE_YEAR = 365 * 24 * 60 * 60;

        private NCalc.Expression _cachedNCalcExpression = null;

        // Parameters are NCalc Parameters used in the call to NCalc.Evaluate()
        private Dictionary<string, object> parameters = new Dictionary<string, object>();

        // Resolved are the Symbol's that have been found (from the References)
        private Dictionary<string, UserSymbol> resolved = new Dictionary<string, UserSymbol>();
        // References are the parsed tokens used in the Expr
        private HashSet<string> references = new HashSet<string>();

        public static readonly bool DATE_VALUES_ALLOWED = true;

        public static readonly bool STRING_VALUES_ALLOWED = true;

        public Expression() { }

        public Expression (Expression cloneMe, ISequenceEntity context, Action<Expression> validator = null) {
            Definition = cloneMe.Definition;
            SymbolBroker = Logic.SymbolBroker.Instance;
            Symbol = cloneMe.Symbol;
            Type = cloneMe.Type;
            Range = cloneMe.Range;
            Default = cloneMe.Default;
            AutoValue = cloneMe.AutoValue;
            DefaultString = cloneMe.DefaultString;
            Validator = validator;
            Context = context;
        }

        public Expression(string definition, ISequenceEntity context) {
            Definition = definition;
            Context = context;
            SymbolBroker = Logic.SymbolBroker.Instance;
        }

        public Expression(string definition, ISequenceEntity context, UserSymbol symbol) {
            if (symbol.Expr is Expression expr) {
                DefaultString = expr.DefaultString;
                Default = expr.Default;
            }
            Definition = definition;
            Context = context;
            Symbol = symbol;
            SymbolBroker = Logic.SymbolBroker.Instance;
        }

        public ISequenceEntity Context { get; set; }
        public double Default {
            get => field;
            set {
                field = value;
                RaisePropertyChanged();
            }
        } = double.NaN;

        public double AutoValue {
            get => field;
            set {
                field = value;
                RaisePropertyChanged();
            }
        } = double.NaN;

        public bool IsValid {
            get;
            set {
                field = value;
                RaisePropertyChanged();
            }
        } = false;

        public string DefaultString {
            // First things first; this Property is only used if Definition is empty
            get {
                // If this is a String Expression and Definition is empty, use empty string
                // If Definition is otherwise Empty, use DefaultString field (localized or not)
                // Otherwise, use the actual Default value
                try {
                    if (Type == "String" && string.IsNullOrWhiteSpace(Definition)) {
                        return "";
                    } else if ((Value == AutoValue || !IsValid) && !string.IsNullOrWhiteSpace(field)) {
                        if (field.StartsWith("Lbl")) {
                            return $"{Loc.Instance[field]}";
                        } else if (field.StartsWith("{")) {
                            // Don't add braces if already in {curly braces} format
                            return field;
                        } else {
                            return "{" + field + "}";
                        }
                    } else {
                        return "{" + Default.ToString(CultureInfo.InvariantCulture) + "}";
                    }
                } finally {
                }
            }
            set {
                field = value;
                RaisePropertyChanged(nameof(DefaultString));
            }
        }

        [JsonProperty]
        public virtual string Definition {
            get {
                return field;
            }
            set {
                if (value == null) return;
                value = value.Trim();

                if (value.Length == 0) {
                    IsExpression = false;
                    if (!double.IsNaN(Default)) {
                        Value = Default;
                    } else {
                        Value = Double.NaN;
                    }
                    field = value;
                    _cachedNCalcExpression = null;
                    parameters.Clear();
                    resolved.Clear();
                    references.Clear();
                    Error = null;
                    ForceAnnotated = false;
                    RaisePropertyChanged(nameof(Error));
                    RaisePropertyChanged(nameof(IsAnnotated));
                    RaisePropertyChanged(nameof(DefaultString));
                    return;
                }

                Double result;

                if (value != field && IsExpression) {
                    // The value has changed.  Clear what we had...
                    foreach (var symKvp in Resolved) {
                        UserSymbol s = symKvp.Value;
                        if (s != null) {
                            symKvp.Value.RemoveConsumer(this);
                        }
                    }
                    resolved.Clear();
                    parameters.Clear();
                }

                field = value;
                _cachedNCalcExpression = null;

                if (Double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result)) {
                    field = String.Format(CultureInfo.InvariantCulture, "{0:0.#######}", result);
                    Error = null;
                    IsExpression = false;

                    // Add range check here...
                    if (Range != null) {
                        CheckRange(result);
                    }

                    Value = result;

                    // Notify consumers
                    if (Symbol != null && !String.IsNullOrEmpty(Symbol.Identifier)) {
                        UserSymbol.SymbolDirty(Symbol);
                        Symbol.Validate();
                    } else {
                        // We always want to show the result if not a Symbol
                        //IsExpression = true;
                    }
                    // The following lines make no sense to me; it would capture literally {ddd} where d are digits.
                    // The "should be" would match any double and would be covered by previous clause
                    // Leaving this here temporarily in case something comes to mind
                    //} else if (Regex.IsMatch(value, "{(\\d+)}")) { // Should be /^\d*\.?\d*$/
                    //    IsExpression = false;
                } else {
                    IsExpression = true;

                    // Evaluate just so that we can parse the expression
                    NCalc.Expression e = new NCalc.Expression(value, NCalc.ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
                    e.Parameters = new Dictionary<string, object>();
                    IsSyntaxError = false;
                    try {
                        e.Evaluate();
                    } catch (NCalc.Exceptions.NCalcParserException) {
                        // We need to report syntax error
                        Error = Loc.Instance["LblSyntaxError"];
                        return;
                    } catch (Exception) {
                        // That's ok, because we just want to find the symbol references
                    }

                    // Find the parameters used
                    references.Clear();

                    foreach (var p in e.GetParameterNames()) {
                        references.Add(p);
                    }

                    // References now holds all of the CV's used in the expression
                    parameters.Clear();
                    resolved.Clear();
                    Evaluate();
                    if (Symbol != null) {
                        UserSymbol.SymbolDirty(Symbol);
                        Symbol.Validate();
                    }
                }
                RaisePropertyChanged(nameof(Definition));
                RaisePropertyChanged(nameof(Value));
                RaisePropertyChanged(nameof(ValueString));
                RaisePropertyChanged(nameof(StringValue));
                RaisePropertyChanged(nameof(IsAnnotated));
                RaisePropertyChanged(nameof(DefaultString));
            }
        } = "";

        public bool Dirty { get; set; }
        public virtual string Error {
            get => field;
            set {
                if (value != field) {
                    field = value;
                    RaisePropertyChanged(nameof(ValueString));
                    RaisePropertyChanged(nameof(IsExpression));
                    RaisePropertyChanged(nameof(IsAnnotated));
                    RaisePropertyChanged(nameof(Error));
                    RaisePropertyChanged(nameof(StringValue));
                    RaisePropertyChanged(nameof(InfoButtonColor));
                }
            }
        }

        public string ExprErrors {
            get {
                if (Error == null) {
                    return Loc.Instance["NoErrors"];
                } else if (JustWarnings(Error)) {
                    return Error; // string.Format(Loc.Instance["LblWarnings"], Error);
                } else {
                    return Error; // string.Format(Loc.Instance["LblErrors"], Error);
                }
            }
        }

        public bool ForceAnnotated { get; set; } = false;
        public bool GlobalVolatile { get; set; } = false;
        public bool HasError => !string.IsNullOrEmpty(Error);
        public SolidColorBrush InfoButtonColor {
            get {
                if (Error == null) return new SolidColorBrush(Colors.White);
                return JustWarnings(Error) ?
                    new SolidColorBrush(Colors.Orange) :
                    new SolidColorBrush(Colors.Red);
            }
        }

        public bool IsAnnotated {
            get => IsExpression || ForceAnnotated || Error != null;
        }

        public bool IsExpression { get; set; } = false;
        public bool IsSyntaxError { get; set; } = false;
        public IReadOnlyDictionary<string, object> Parameters => parameters.AsReadOnly();
        /// <summary>
        /// Specifies the allowed numeric range for this expression.
        /// 
        /// The array must contain exactly three elements:
        /// 
        ///     Range[0] = Minimum value
        ///     Range[1] = Maximum value
        ///     Range[2] = Boundary flags (encoded as an integer bitmask)
        ///
        /// The boundary flags determine whether the min/max boundaries are
        /// inclusive or exclusive. They use the ExpressionRange flags:
        ///
        ///     ExpressionRange.MIN_EXCLUSIVE = 1   // bit 0
        ///     ExpressionRange.MAX_EXCLUSIVE = 2   // bit 1
        ///
        /// Meaning of Range[2]:
        ///
        ///     0 (binary 00) → Min inclusive, Max inclusive
        ///     1 (binary 01) → Min exclusive, Max inclusive
        ///     2 (binary 10) → Min inclusive, Max exclusive
        ///     3 (binary 11) → Min exclusive, Max exclusive
        ///
        /// Example:
        ///
        ///     Range = new double[] { 0, 10, 3 };
        ///
        /// Means: 0 < value < 10 (both sides exclusive).
        ///
        /// This field is optional; if null, no range checking occurs.
        /// </summary>
        public double[]? Range { get; set; }

        public IReadOnlyCollection<string> References => references;
        public IReadOnlyDictionary<string, UserSymbol> Resolved => resolved.AsReadOnly();
        public string StringValue { get; set; }
        public UserSymbol Symbol { get; set; } = null;
        public ISymbolBroker SymbolBroker { get; set; }
        public string Type { get; set; } = "double";
        public Action<Expression> Validator { get; set; }
        public virtual double Value {
            get {
                if (double.IsNaN(field) && !double.IsNaN(Default)) {
                    return Default;
                }
                return field;
            }
            set {
                if (value != field) {
                    if ("int".Equals(Type)) {
                        if (StringValue != null) {
                            Error = Loc.Instance["LblMustBeInteger"];
                        }
                        ForceAnnotated = false;
                        if (Definition.Length > 0 && Double.Floor(value) != value) {
                            value = Double.Floor(value);
                            ForceAnnotated = true;
                        }
                        RaisePropertyChanged(nameof(IsAnnotated));
                    }
                    field = value;
                    if (Range != null) {
                        CheckRange((double)value);
                    }
                    if (Validator != null) {
                        Validator(this);
                    }
                    RaisePropertyChanged(nameof(StringValue));
                    RaisePropertyChanged(nameof(Value));
                    RaisePropertyChanged(nameof(ValueString));
                    RaisePropertyChanged(nameof(IsExpression));
                }
            }
        } = double.NaN;

        public string ValueString {
            get {
                if (Error != null) return Error;
                if (double.IsNegativeInfinity(Value)) {
                    return StringValue;
                }
                long start = DateTimeOffset.Now.ToUnixTimeSeconds() - ONE_YEAR;
                long end = start + (2 * ONE_YEAR);
                if (DATE_VALUES_ALLOWED && Value > start && Value < end) {
                    var local = CoreUtil.UnixTimeStampToDateTime(Value).ToLocalTime();
                    var today = DateTime.Today;
                    if (local.Date == today.AddDays(1)) {
                        return local.ToShortTimeString() + " " + Loc.Instance["LblTomorrow"];
                    } else if (local.Date == today.AddDays(-1)) {
                        return local.ToShortTimeString() + " " + Loc.Instance["LblYesterday"];
                    } else if (local.Date == today) {
                        return local.ToShortTimeString();
                    } else
                        return local.ToString(CultureInfo.CurrentCulture);
                } else {
                    if ((Value == AutoValue) || (!double.IsNaN(Default) && Value == Default)) {
                        return DefaultString;
                    } else if (Symbol is Variable v && !v.Executed) {
                        return Loc.Instance["LblNotEvaluated"];
                    } else if (double.IsNaN(Value)) {
                        return "";
                    }

                    return Value.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        public bool Volatile { get; set; } = false;
        private void AddError(string s) {
            if (Error == null) {
                Error = s;
            } else {
                Error = Error + "; " + s;
            }
        }

        private void AddParameter(string reference, object value) {
            parameters.Add(reference, value);
        }

        private void CheckRange(double value) {
            string rangeString = RangeString(value);
            if (rangeString != null) {
                Error = rangeString;
            }
        }

        public string? RangeString(double? value) {
            if (Range?.Length < 3) { return null; }

            int r = Convert.ToInt32(Range[2], CultureInfo.InvariantCulture);

            bool minExclusive = (r & ExpressionRange.MIN_EXCLUSIVE) == ExpressionRange.MIN_EXCLUSIVE;
            bool maxExclusive = (r & ExpressionRange.MAX_EXCLUSIVE) == ExpressionRange.MAX_EXCLUSIVE;

            double min = Range[0] + (((r & ExpressionRange.MIN_EXCLUSIVE) == ExpressionRange.MIN_EXCLUSIVE) ? 1e-8 : 0);
            double max = Range[1] == 0 ? double.MaxValue : Range[1] - (((r & ExpressionRange.MAX_EXCLUSIVE) == ExpressionRange.MAX_EXCLUSIVE) ? 1e-8 : 0);

            bool outOfRange =
                (value == null) ? true :
                ((minExclusive ? value <= min : value < min) ||
                (maxExclusive ? value >= max : value > max));

            if (!outOfRange) { return null; }

            string msgKey;

            if (Range[1] == 0) {
                if (minExclusive) {
                    msgKey = "Lbl_Expressions_CheckRange_RangeGreaterThan";
                } else {
                    msgKey = "Lbl_Expressions_CheckRange_RangeGreaterThanOrEquals";
                }
            } else if (!minExclusive && !maxExclusive) {
                msgKey = "Lbl_Expressions_CheckRange_RangeInclusiveInclusive";
            } else if (!minExclusive && maxExclusive) {
                msgKey = "Lbl_Expressions_CheckRange_RangeInclusiveExclusive";
            } else if (minExclusive && !maxExclusive) {
                msgKey = "Lbl_Expressions_CheckRange_RangeExclusiveInclusive";
            } else {
                msgKey = "Lbl_Expressions_CheckRange_RangeExclusiveExclusive";
            }
            return string.Format(CultureInfo.InvariantCulture, Loc.Instance[msgKey], Range[0], Range[1]);
        }
        private void ExtensionFunction(string name, FunctionArgs args) {
            try {
                SymbolBroker.InvokeFunction(name, args, out var result, out var isVolatile);
                args.Result = result;

                if (isVolatile) {
                    // Always check again on validation
                    GlobalVolatile = true;
                }
            } catch (Exception ex) {
                // Any renamed functions in Powerups 3 upgrades will generate log entries every 5 seconds, spamming the log
                // These are very hard to recognize in the upgrader, as they may be buried inside complex Expressions
                // The UI will mark these with a red triangle, i.e. the error isn't buried, it's just not logged over and over
                LogOnce($"Error evaluating function {name}: {ex.Message}");
                throw new NCalcEvaluationException(ex.Message);
            }
        }

        private void Resolve(string reference, UserSymbol sym) {
            parameters.Remove(reference);
            resolved.Remove(reference);
            if (sym.Expr.Error == null) {
                resolved.Add(reference, sym);
                if (sym.Expr.Value == double.NegativeInfinity) {
                    AddParameter(reference, sym.Expr.StringValue);
                } else
                if (!Double.IsNaN(sym.Expr.Value)) {
                    AddParameter(reference, sym.Expr.Value);
                }
            }
        }

        public static bool JustWarnings(string error) {
            string[] errors = error.Split(";");
            bool red = false;
            bool orange = false;
            foreach (string e in errors) {
                // Note "External" not used currently
                if (e.Contains(Loc.Instance["LblNotEvaluated"]) || e.Contains("External")) {
                    orange = true;
                } else {
                    red = true;
                }
            }
            if (orange && !red) return true;
            return false;
        }
        public static void ValidateExpressions(IList<string> issues, params Expression[] exprs) {
            foreach (Expression expr in exprs) {
                expr.Validate();
                if (expr != null && expr.Error != null && !Expression.JustWarnings(expr.Error)) {
                    issues.Add(expr.Error);
                }
            }
        }

        public void Evaluate() {
            Evaluate(false);
        }

        public void Evaluate(bool ignoreRoot) {
            // It's possible that this gets called from multiple threads (e.g., UI thread, Sequencer thread, and Validate thread)
            lock (this) {
                if (!IsExpression && Error == null) {
                    // If there was an error, we still want to validate (it might have failed range validation, for example)
                    return;
                }
                if (Definition.Length == 0) {
                    IsExpression = false;
                    RaisePropertyChanged(nameof(Value));
                    RaisePropertyChanged(nameof(ValueString));
                    RaisePropertyChanged(nameof(StringValue));
                    RaisePropertyChanged(nameof(IsExpression));
                    return;
                }
                if (Context == null) return;
                if (!ignoreRoot && !UserSymbol.IsAttachedToRoot(Context)) {
                    return;
                }

                if (Volatile || GlobalVolatile) {
                    IList<string> volatiles = new List<string>();
                    foreach (KeyValuePair<string, UserSymbol> kvp in Resolved) {
                        if (kvp.Value == null || kvp.Value.Expr.GlobalVolatile) {
                            volatiles.Add(kvp.Key);
                        }
                    }
                    foreach (string key in volatiles) {
                        resolved.Remove(key);
                        parameters.Remove(key);
                    }
                }

                Volatile = GlobalVolatile;

                //ImageVolatile = false;

                StringValue = null;

                if (Parameters.Count < Resolved.Count) {
                    parameters.Clear();
                    resolved.Clear();
                }

                if (SymbolBroker == null && Context != null) {
                    SymbolBroker = Context.SymbolBroker;
                }
                if (SymbolBroker == null && Symbol != null) {
                    SymbolBroker = Symbol.SymbolBroker;
                }

                // First, validate References
                foreach (string sRef in References) {
                    UserSymbol sym;
                    string symReference = sRef;
                    // Remember if we have any image data
                    //if (!ImageVolatile && symReference.StartsWith("Image_")) {
                    //    ImageVolatile = true;
                    //}
                    bool found = Resolved.TryGetValue(symReference, out sym);
                    if (!found || sym == null) {
                        // !found -> couldn't find it; sym == null -> it's a DataSymbol
                        if (!found) {
                            sym = FindSymbol(symReference, Symbol?.Parent ?? Context.Parent);
                        }
                        if (sym != null) {
                            // Link Expression to the Symbol
                            Resolve(symReference, sym);
                            sym.AddConsumer(this);
                        } else if (SymbolBroker != null) {
                            found = false;
                            // Try SymbolBroker
                            object val = null;
                            if (!found && SymbolBroker.TryGetValue(symReference, out val)) {
                                // We don't want these resolved, just added to Parameters
                                resolved.Remove(symReference);
                                resolved.Add(symReference, null);
                                parameters.Remove(symReference);
                                AddParameter(symReference, val);
                                Volatile = true;
                            } else if (val is AmbiguousSymbol a) {
                                StringBuilder sb = new StringBuilder("'" + a.Key + "' " + Loc.Instance["LblIsAmbiguous"]);
                                Symbol[] symbols = a.Symbols;
                                for (int i = 0; i < symbols.Length; i++) {
                                    sb.Append(" " + symbols[i].Category + '_' + symReference);
                                    if (i < symbols.Length - 1) {
                                        sb.Append("; ");
                                    }
                                }
                                Error = sb.ToString();
                                return;
                            }
                        } else if (Context != null && Context.Parent is not IImmutableContainer) {
                           // This is fine if we're in a SmartExposure, TakeManyExposures, etc.
                           Logger.Warning("SymbolBroker not found in " + Context.Name);
                        }
                    }
                }

                NCalc.Expression e;
                if (_cachedNCalcExpression != null) {
                    e = _cachedNCalcExpression;
                } else {
                    e = new NCalc.Expression(Definition, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
                    e.EvaluateFunction += ExtensionFunction;
                    _cachedNCalcExpression = e;
                }
                e.Parameters = parameters;

                if (e.HasErrors()) {
                    Error = Loc.Instance["LblSyntaxError"];
                    return;
                }

                Error = null;
                try {
                    if (Parameters.Count != References.Count) {
                        foreach (string r in References) {
                            string symReference = r;
                            if (!Parameters.ContainsKey(symReference)) {
                                // Not defined or evaluated
                                UserSymbol s = FindSymbol(symReference, Symbol?.Parent ?? Context.Parent);
                                if (s is Variable sv && !sv.Executed) {
                                    AddError(Loc.Instance["LblNotEvaluated"] + ": " + r);
                                    //                           } else if (r.StartsWith("_")) {
                                    //                               AddError("Reference: " + r);
                                } else {
                                    //                                if (r.StartsWith('$') && ext && validateOnly) {
                                    //                                    AddError("External: " + symReference);
                                    //                                } else {
                                    AddError(Loc.Instance["LblUndefined"] + ": " + r);
                                    //                                }
                                }
                            }
                        }
                        RaisePropertyChanged(nameof(Error));
                        RaisePropertyChanged(nameof(ValueString));
                        RaisePropertyChanged(nameof(StringValue));
                        RaisePropertyChanged(nameof(Value));
                    } else {
                        Error = null;
                        object eval = e.Evaluate();
                        // We got an actual value
                        if (eval is Boolean b) {
                            Value = b ? 1 : 0;
                        } else {
                            try {
                                Value = Convert.ToDouble(eval, CultureInfo.InvariantCulture);
                                // Validate numeric values
                                if (Range != null) {
                                    CheckRange(Value);
                                }
                            } catch (Exception) {
                                string str = eval as string;
                                if (STRING_VALUES_ALLOWED) {
                                    if (str != null) {
                                        StringValue = str;
                                        Value = double.NegativeInfinity;
                                    } else {
                                        Error = Loc.Instance["LblSyntaxError"];
                                    }
                                } else {
                                    // This can't happen as we allow strings.  Don't localize.  But don't delete the if just in case...
                                    Error = (str != null) ? "Strings are now allowed as values" : Loc.Instance["LblSyntaxError"];
                                }
                            }
                        }
                        RaisePropertyChanged(nameof(Error));
                        RaisePropertyChanged(nameof(StringValue));
                        RaisePropertyChanged(nameof(ValueString));
                        RaisePropertyChanged(nameof(Value));
                    }

                } catch (NCalc.Exceptions.NCalcParameterNotDefinedException ex) {
                    Error = Loc.Instance["LblUndefined"] + ": " + ex.ParameterName;
                } catch (NCalc.Exceptions.NCalcEvaluationException ex) {
                    Error = ex.Message;
                    return;
                } catch (NCalc.Exceptions.NCalcParserException) {
                    Error = Loc.Instance["LblSyntaxError"];
                    return;
                } catch (Exception ex) {
                    Error = Loc.Instance["LblError"] + ": " + ex.Message;
                    Logger.Error("Exception evaluating " + Definition + ": " + ex.Message);
                }
                Dirty = false;

            }
        }

        private static HashSet<string> LoggedOnce = new HashSet<string>();
        public static void LogOnce(string message) {
            if (LoggedOnce.Contains(message)) return;
            Logger.Warning(message);
            LoggedOnce.Add(message);
        }

        public void ReferenceRemoved(UserSymbol sym) {
            // A definition we use was removed
            string identifier = sym.Identifier;
            parameters.Remove(identifier);
            resolved.Remove(identifier);
            Evaluate();
        }

        public void Refresh() {
            parameters.Clear();
            resolved.Clear();
            Evaluate();
        }

        public void RemoveParameter(string identifier) {
            parameters.Remove(identifier);
            resolved.Remove(identifier);
            Evaluate();
        }

        public override string ToString() {
            string id = Symbol != null
        ? (Symbol.Name + ": " + Symbol.Identifier)
        : Context?.Name;

            if (Error != null) {
                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"'{Definition}' in {id}, References: {References.Count}, Error: {Error}"
                );
            } else if (Definition.Length == 0) {
                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"Undefined{(Context != null ? " in " + Context.Name : "")}{(Validator != null ? " (with Validator)" : "")}"
                );
            }

            return string.Create(
                CultureInfo.InvariantCulture,
                $"Expression: {Definition} in {id}, {string.Join(", ", Parameters.Select(a => a.Key + " = " + a.Value))}, Value: {ValueString}"
            );
        }

        public void Validate(IList<string> issues) {
            if (Context == null) {
                return;
            }
            if (Error != null || Volatile) {
                if (Definition != null && Definition.Length == 0 && Value == Default) {
                    Error = null;
                }
                Evaluate(true);
                foreach (KeyValuePair<string, UserSymbol> kvp in Resolved) {
                    if (kvp.Value == null || kvp.Value.Expr.GlobalVolatile) {
                        GlobalVolatile = true;
                    }
                }
            } else if (double.IsNaN(Value) && Definition?.Length > 0) {
                Error = Loc.Instance["LblNotEvaluated"];
            } else if (Resolved.Count != References.Count) {
                // Why would this happen... track down?
                Evaluate();
            } else if (Definition.Length != 0 && Value == Default && Error == null) {
                // This seems very wrong to me; need to figure it out
                Evaluate(true);
            }
        }

        public void Validate() {
            Validate(null);
        }
    }
}
