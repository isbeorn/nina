using NCalc;
using NCalc.Handlers;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Expressions;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using static NINA.Sequencer.Logic.UserSymbol;

namespace NINA.Sequencer.Logic {
    [JsonObject(MemberSerialization.OptIn)]
    public class Expression : BaseINPC {
        /// <summary>
        /// Used by the JSON serializer
        /// </summary>
        public Expression() { }
        public Expression (Expression cloneMe, ISequenceEntity context, Action<Expression> validator = null) {
            Definition = cloneMe.Definition;
            SymbolBroker = cloneMe.SymbolBroker;
            Symbol = cloneMe.Symbol;
            Range = cloneMe.Range;
            Default = cloneMe.Default;
            DefaultString = cloneMe.DefaultString;
            Validator = validator;
            Context = context;
        }

        public Expression(string definition, ISequenceEntity context) {
            Definition = definition;
            Context = context;
        }

        public Expression(string definition, ISequenceEntity context, UserSymbol symbol) {
            if (symbol.Expr is Expression expr) {
                DefaultString = expr.DefaultString;
                Default = expr.Default;
            }
            Definition = definition;
            Context = context;
            Symbol = symbol;
        }

        public static readonly bool STRING_VALUES_ALLOWED = true;
        public static readonly bool DATE_VALUES_ALLOWED = true;

        public ISymbolBroker SymbolBroker;

        public bool HasError => !string.IsNullOrEmpty(Error);
 
        private string _error;
        public virtual string Error {
            get => _error;
            set {
                if (value != _error) {
                    _error = value;
                    RaisePropertyChanged(nameof(ValueString));
                    RaisePropertyChanged(nameof(IsExpression));
                    RaisePropertyChanged(nameof(IsAnnotated));
                    RaisePropertyChanged(nameof(Error));
                    RaisePropertyChanged(nameof(StringValue));
                    RaisePropertyChanged(nameof(InfoButtonColor));
                }
            }
        }

        public bool Dirty { get; set; }
        public ISequenceEntity Context { get; set; }

        public Action<Expression> Validator;

        private double _default = Double.NaN;
        public double Default {
            get => _default;
            set {
                _default = value;
                RaisePropertyChanged();
            }
        }

        public string Type { get; set; } = "double";

        public bool Volatile { get; set; } = false;
        public bool GlobalVolatile { get; set; } = false;

        private string defaultString = null;
        public string DefaultString {
            get {
                if (Double.IsNaN(Default) && Definition.Length == 0) {
                    return "";
                } else if (string.IsNullOrWhiteSpace(defaultString)) {
                    return Default.ToString(CultureInfo.InvariantCulture);
                } else if (defaultString.StartsWith("Lbl")) {
                    return $"{{{Core.Locale.Loc.Instance[defaultString]}}}";
                }
                return defaultString;
            }
            set {
                defaultString = value;
            }
        }

        public double[]? Range { get; set; }
        public bool IsExpression { get; set; } = false;
        public bool IsSyntaxError { get; set; } = false;
        public bool IsAnnotated {
            get => IsExpression || ForceAnnotated || Error != null;
            set { }
        }

        public bool ForceAnnotated { get; set; } = false;
        public string StringValue { get; set; }

        private double _value = Double.NaN;
        public virtual double Value {
            get {
                if (double.IsNaN(_value) && !double.IsNaN(Default)) {
                    return Default;
                }
                return _value;
            }
            set {
                if (value != _value) {
                    if ("int".Equals(Type)) {
                        if (StringValue != null) {
                            Error = "Value must be an Integer";
                        }
                        ForceAnnotated = false;
                        if (Definition.Length > 0 && Double.Floor(value) != value) {
                            value = Double.Floor(value);
                            ForceAnnotated = true;
                        }
                        RaisePropertyChanged(nameof(IsAnnotated));
                    }
                    _value = value;
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
        }

        private void CheckRange(double value) {
            if (Range?.Length < 3) { return; }

            int r = Convert.ToInt32(Range[2], CultureInfo.InvariantCulture);
            double min = Range[0] + (((r & ExpressionRange.MIN_EXCLUSIVE) == ExpressionRange.MIN_EXCLUSIVE) ? 1e-8 : 0);
            double max = Range[1] - (((r & ExpressionRange.MAX_EXCLUSIVE) == ExpressionRange.MAX_EXCLUSIVE) ? 1e-8 : 0);
            if (value < min || (max != 0 && value > max)) {
                if (r == 0) {
                    if (max == 0) {
                        Error = "Range: >= " + min;
                    } else {
                        Error = "Range: " + min + " < value < " + max;
                    }
                } else {
                    Error = "Value must be " + (((r & 1) == 1) ? "greater than " : "between ") + Range[0] + " and less than " + (((r & 2) == 2) ? "" : "or equal to ") + Range[1];
                }
            }
        }

        public SolidColorBrush InfoButtonColor {
            get {
                if (Error == null) return new SolidColorBrush(Colors.White);
                return JustWarnings(Error) ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Red);
            }
            set { }
        }

        public static bool JustWarnings(string error) {
            string[] errors = error.Split(";");
            bool red = false;
            bool orange = false;
            foreach (string e in errors) {
                if (e.Contains("Not evaluated") || e.Contains("External")) {
                    orange = true; ;
                } else {
                    red = true;
                }
            }
            if (orange && !red) return true;
            return false;
        }
        public string ExprErrors {
            get {
                if (Error == null) {
                    return "No errors in Expression";
                } else if (JustWarnings(Error)) {
                    return "Warning(s): " + Error;
                } else {
                    return "Error(s): " + Error;
                }
            }
            set { }
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
                Error = "Not evaluated";
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

        public static void ValidateExpressions(IList<string> issues, params Expression[] exprs) {
            foreach (Expression expr in exprs) {
                expr.Validate();
                if (expr != null && expr.Error != null && !Expression.JustWarnings(expr.Error)) {
                    issues.Add(expr.Error);
                }
            }
        }

        public UserSymbol Symbol { get; set; } = null;

        private static readonly int ONE_YEAR = 365 * 24 * 60 * 60;

        public string ValueString {
            get {
                if (Error != null) return Error;
                if (Value is double.NegativeInfinity) {
                    return StringValue;
                }
                long start = DateTimeOffset.Now.ToUnixTimeSeconds() - ONE_YEAR;
                long end = start + (2 * ONE_YEAR);
                if (DATE_VALUES_ALLOWED && Value > start && Value < end) {
                    var local = ConvertFromUnixTimestamp(Value).ToLocalTime();
                    var today = DateTime.Today;
                    if (local.Date == today.AddDays(1)) {
                        return local.ToShortTimeString() + " tomorrow";
                    } else if (local.Date == today.AddDays(-1)) {
                        return local.ToShortTimeString() + " yesterday";
                    } else if (local.Date == today) {
                        return local.ToShortTimeString();
                    } else
                        return local.ToString(CultureInfo.CurrentCulture);
                } else {
                    if (!double.IsNaN(Default) && Value == Default) {
                        return DefaultString;
                    }

                    return Value.ToString(CultureInfo.InvariantCulture);
                }
            }
            set { }
        }

        // References are the parsed tokens used in the Expr
        private HashSet<string> references { get; set; } = new HashSet<string>();
        public IReadOnlyCollection<string> References => references;

        // Resolved are the Symbol's that have been found (from the References)
        private Dictionary<string, UserSymbol> resolved = new Dictionary<string, UserSymbol>();
        public IReadOnlyDictionary<string, UserSymbol> Resolved => resolved.AsReadOnly();

        // Parameters are NCalc Parameters used in the call to NCalc.Evaluate()
        private Dictionary<string, object> parameters = new Dictionary<string, object>();
        public IReadOnlyDictionary<string, object> Parameters => parameters.AsReadOnly();

        private string definition = "";
        [JsonProperty]
        public virtual string Definition {
            get {
                return definition;
            }
            set {
                if (value == null) return;
                if (value == definition) return;
                value = value.Trim();

                if (value.Length == 0) {
                    IsExpression = false;
                    if (!double.IsNaN(Default)) {
                        Value = Default;
                    } else {
                        Value = Double.NaN;
                    }
                    definition = value;
                    parameters.Clear();
                    resolved.Clear();
                    references.Clear();
                    Error = null;
                    ForceAnnotated = false;
                    RaisePropertyChanged(nameof(Error));
                    RaisePropertyChanged(nameof(IsAnnotated));
                    return;
                }

                Double result;

                if (value != definition && IsExpression) {
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

                definition = value;

                if (Double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result)) {
                    definition = String.Format(CultureInfo.InvariantCulture, "{0:0.#######}", result);
                    Error = null;
                    IsExpression = false;
                    Value = result;

                    

                    // Notify consumers
                    if (Symbol != null) {
                        UserSymbol.SymbolDirty(Symbol);
                        Symbol.Validate();
                    } else {
                        // We always want to show the result if not a Symbol
                        //IsExpression = true;
                    }
                } else if (Regex.IsMatch(value, "{(\\d+)}")) { // Should be /^\d*\.?\d*$/
                    IsExpression = false;
                } else {
                    IsExpression = true;

                    // Evaluate just so that we can parse the expression
                    NCalc.Expression e = new NCalc.Expression(value, NCalc.ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
                    e.Parameters = new Dictionary<string, object>();
                    IsSyntaxError = false;
                    try {
                        e.Evaluate();
                    } catch (NCalc.Exceptions.NCalcParserException) {
                        // We should expect this, since we're just trying to find the parameters used
                        Error = "Syntax Error";
                        return;
                    } catch (Exception) {
                        // That's ok
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
            }
        }
        public void RemoveParameter(string identifier) {
            parameters.Remove(identifier);
            resolved.Remove(identifier);
            Evaluate();
        }

        public void ReferenceRemoved(UserSymbol sym) {
            // A definition we use was removed
            string identifier = sym.Identifier;
            parameters.Remove(identifier);
            resolved.Remove(identifier);
            Evaluate();
        }
        private void AddParameter(string reference, object value) {
            parameters.Add(reference, value);
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
        public void Refresh() {
            parameters.Clear();
            resolved.Clear();
            Evaluate();
        }

        private void AddError(string s) {
            if (Error == null) {
                Error = s;
            } else {
                Error = Error + "; " + s;
            }
        }

        public void Evaluate() {
            Evaluate(false);
        }

        public void Evaluate(bool validateOnly) {
            if (!IsExpression) {
                //Error = null;
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
            if (!UserSymbol.IsAttachedToRoot(Context)) {
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

            // External, don't report error during validation
            bool ext = false;

            if (SymbolBroker == null && Context != null) {
                SymbolBroker = Context.SymbolBroker;
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
                            StringBuilder sb = new StringBuilder("The variable '" + a.Key + "' is ambiguous, use one of");
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
                    } else {
                        Logger.Warning("SymbolBroker not found in " + Context.Name);
                    }
                }
            }

            NCalc.Expression e = new NCalc.Expression(Definition, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
            e.EvaluateFunction += ExtensionFunction;
            e.Parameters = parameters;

            if (e.HasErrors()) {
                Error = "Syntax Error";
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
                                AddError("Not evaluated: " + r);
                            } else if (r.StartsWith("_")) {
                                AddError("Reference: " + r);
                            } else {
                                if (r.StartsWith('$') && ext && validateOnly) {
                                    AddError("External: " + symReference);
                                } else {
                                    AddError("Undefined: " + r);
                                }
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
                        } catch (Exception) {
                            string str = eval as string;
                            if (STRING_VALUES_ALLOWED) {
                                if (str != null) {
                                    StringValue = str;
                                    Value = double.NegativeInfinity;
                                } else {
                                    Error = "Syntax error";
                                }
                            } else {
                                Error = (str != null) ? "Strings are now allowed as values" : "Syntax error";
                            }
                        }
                    }
                    RaisePropertyChanged(nameof(Error));
                    RaisePropertyChanged(nameof(StringValue));
                    RaisePropertyChanged(nameof(ValueString));
                    RaisePropertyChanged(nameof(Value));
                }

            } catch (NCalc.Exceptions.NCalcParameterNotDefinedException ex) {
                Error = "Undefined: " + ex.ParameterName;
            } catch (Exception ex) {
                if (ex is NCalc.Exceptions.NCalcEvaluationException || ex is NCalc.Exceptions.NCalcParserException) {
                    Error = "Syntax Error";
                    return;
                } else {
                    Error = "Error: " + ex.Message; // "Unknown Error; see log";
                    Logger.Warning("Exception evaluating " + Definition + ": " + ex.Message);
                }
            }
            Dirty = false;
        }
        
        public static DateTime ConvertFromUnixTimestamp(double timestamp) {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }
        public long UnixTimeNow() {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            return (long)timeSpan.TotalSeconds;
        }

        public static Random RNG = new Random();

        public void ExtensionFunction(string name, FunctionArgs args) {
            DateTime dt;
            try {
                if (args.Parameters.Length > 0) {
                    try {
                        var utc = ConvertFromUnixTimestamp(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        dt = utc.ToLocalTime();
                    } catch (Exception) {
                        dt = DateTime.MinValue;
                    }
                } else {
                    dt = DateTime.Now;
                }
                if (name == "now") {
                    args.Result = UnixTimeNow();
                } else if (name == "hour") {
                    args.Result = (int)dt.Hour;
                } else if (name == "minute") {
                    args.Result = (int)dt.Minute;
                } else if (name == "day") {
                    args.Result = (int)dt.Day;
                } else if (name == "month") {
                    args.Result = (int)dt.Month;
                } else if (name == "year") {
                    args.Result = (int)dt.Year;
                } else if (name == "dow") {
                    args.Result = (int)dt.DayOfWeek;
                } else if (name == "dateString") {
                    if (args.Parameters.Length < 2) {
                        throw new ArgumentException();
                    }
                    args.Result = dt.ToString((string)args.Parameters[1].Evaluate());
                } else if (name == "defined") {
                    string str = Convert.ToString(args.Parameters[0].Evaluate());
                    args.Result = SymbolBroker.TryGetValue(str, out _);
                    // Always check again on validation
                    GlobalVolatile = true;
                } else if (name == "startsWith") {
                    string str = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                    string f = Convert.ToString(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                    args.Result = str.StartsWith(f, StringComparison.Ordinal);
                } else if (name == "strLength") {
                    var e = args.Parameters[0].Evaluate();
                    if (e is string es) {
                        args.Result = es.Length;
                    } else {
                        args.Result = -1;
                    }
                } else if (name == "strConcat") {
                    var e = args.Parameters[0].Evaluate().ToString();
                    var i = args.Parameters[1].Evaluate().ToString();
                    if (e is string es && i is string iss) {
                        args.Result = String.Concat(es, iss);
                    } else {
                        args.Result = "";
                    }
                } else if (name == "strAtPos") {
                    var e = args.Parameters[0].Evaluate();
                    var i = args.Parameters[1].Evaluate();
                    if (e is string es && i is int iint && iint >= 0 && iint < es.Length) {
                        args.Result = Convert.ToString(es[iint]);
                    } else {
                        args.Result = "";
                    }
                } else if (name == "random") {
                    args.Result = RNG.NextDouble();
                }
            } catch (Exception ex) {
                Logger.Error("Error evaluating function " + name + ": " + ex.Message);
            }
        }

        public override string ToString() {
            string id = Symbol != null ? (Symbol.Name + ": " + Symbol.Identifier) : Context?.Name;
            if (Error != null) {
                return $"'{Definition}' in {id}, References: {References.Count}, Error: {Error}";
            } else if (Definition.Length == 0) {
                return "Undefined" + (Context != null ? " in " + Context.Name : "") + (Validator != null ? " (with Validator)" : "");
            }
            return $"Expression: {Definition} in {id}, References: {References.Count}, Value: {ValueString}";
        }

    }
}
