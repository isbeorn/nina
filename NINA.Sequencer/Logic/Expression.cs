using Newtonsoft.Json;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NCalc;
using System.Windows.Media;
using static NINA.Sequencer.Logic.Symbol;
using System.Threading;
using NCalc.Handlers;
using NINA.Astrometry;
using NINA.Sequencer.SequenceItem;

namespace NINA.Sequencer.Logic {
    [JsonObject(MemberSerialization.OptIn)]
    public class Expression : BaseINPC {

        public Expression() {

        }

        public Expression (Expression cloneMe) {
            Definition = cloneMe.Definition;
            Context = cloneMe.Context;
            Symbol = cloneMe.Symbol;
            Validator = cloneMe.Validator;
            Range = cloneMe.Range;
            Default = cloneMe.Default;
        }

        public Expression(string definition, ISequenceEntity context) {
            Definition = definition;
            Context = context;
        }

        public Expression(string definition, ISequenceEntity context, Symbol symbol) {
            if (symbol.Expr is Expression expr) {
                DefaultString = expr.DefaultString;
                Default = expr.Default;
            }
            Definition = definition;
            Context = context;
            Symbol = symbol;
        }

        public ISymbolBrokerVM SymbolBroker;

        public bool HasError => string.IsNullOrEmpty(Error);
 
        private string _error;
        public virtual string Error {
            get => _error;
            set {
                if (value != _error) {
                    _error = value;
                    RaisePropertyChanged("ValueString");
                    RaisePropertyChanged("IsExpression");
                    RaisePropertyChanged("IsAnnotated");
                    RaisePropertyChanged("Error");
                    RaisePropertyChanged("StringValue");
                    RaisePropertyChanged("InfoButtonColor");
                    RaisePropertyChanged("InfoButtonChar");
                    RaisePropertyChanged("InfoButtonSize");
                    RaisePropertyChanged("InfoButtonMargin");
                }
            }
        }

        public bool Dirty { get; set; }
        public ISequenceEntity Context { get; set; }

        public Action<Expression>? Validator;

        public double Default { get; set; } = double.NaN;

        private string defaultString = null;
        public string DefaultString {
            get {
                if (Double.IsNaN(Default) && Definition.Length == 0) {
                    return "";
                } else if (defaultString == null) {
                    return Default.ToString();
                } else {
                    return defaultString;
                }
            }
            set {
                defaultString = value;
            }
        }

        public double[]? Range { get; set; }
        public bool IsExpression { get; set; } = false;
        public bool IsSyntaxError { get; set; } = false;
        public bool IsAnnotated {
            get => IsExpression || Error != null;
            set { }
        }
        private double _value = Double.NaN;
        public virtual double Value {
            get {
                if (double.IsNaN(_value) && !double.IsNaN(Default)) {
                    return Default;
                //} else if (double.IsNaN(_value)) {
                //    return 0;
                }
                return _value;
            }
            set {
                if (value != _value) {
                    //if ("Integer".Equals(Type)) {
                    //    if (StringValue != null) {
                    //        Error = "Value must be an Integer";
                    //    }
                    //    value = Double.Floor(value);
                    //}
                    _value = value;
                    if (Range != null) {
                        CheckRange((double)value, Range);
                    } 
                    if (Validator != null) {
                        Validator(this);
                    }
                    //RaisePropertyChanged("StringValue");
                    RaisePropertyChanged("Value");
                    RaisePropertyChanged("ValueString");
                    RaisePropertyChanged("IsExpression");
                    ////RaisePropertyChanged("DockableValue");
                }
            }
        }

        private void CheckRange(double value, double[] range) {
            int r = Convert.ToInt32(Range[2]);
            double min = Range[0] + (((r & ExpressionRange.MIN_EXCLUSIVE) == ExpressionRange.MIN_EXCLUSIVE) ? 1e-8 : 0);
            double max = Range[1] - (((r & ExpressionRange.MAX_EXCLUSIVE) == ExpressionRange.MAX_EXCLUSIVE) ? 1e-8 : 0);
            if (value < min || (max != 0 && value > max)) {
                if (r == 0) {
                    if (max == 0) {
                        Error = "Range: >= " + min;
                        //Error = "Value must be " + min + " or greater";
                    } else {
                        Error = "Range: " + min + "< value < " + max;
                        //Error = "Value must be between " + min + " and " + max;
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
        public string InfoButtonChar {
            get {
                if (Error == null) return "\u24D8"; // "?";
                return "\u26A0";
            }
            set { }
        }
        public double InfoButtonSize {
            get {
                if (Error == null) return 18;
                return 18;
            }
            set { }
        }
        public string InfoButtonMargin {
            get {
                if (Error == null) return "5,4,0,0";
                return "5,2,0,0";
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
            if (Error != null) {  // || Volatile) {
                if (Definition != null && Definition.Length == 0 && Value == Default) {
                    Error = null;
                }
                Evaluate(true);
                ///foreach (KeyValuePair<string, Symbol> kvp in Resolved) {
                //    if (kvp.Value == null || kvp.Value.Expr.GlobalVolatile) {
                //        GlobalVolatile = true;
                //    }
                //}
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

        public Symbol Symbol { get; set; } = null;

        public string ValueString {
            get {
                if (Error != null) return Error;
                //if (Value is double.NegativeInfinity) {
                //    return StringValue;
                //}
                //long start = DateTimeOffset.Now.ToUnixTimeSeconds() - ONE_YEAR;
                //long end = start + (2 * ONE_YEAR);
                //if (Value > start && Value < end) {
                //    DateTime dt = ConvertFromUnixTimestamp(Value).ToLocalTime();
                //    if (dt.Day == DateTime.Now.Day + 1) {
                //        return dt.ToShortTimeString() + " tomorrow";
                //    } else if (dt.Day == DateTime.Now.Day - 1) {
                //        return dt.ToShortTimeString() + " yesterday";
                //    } else
                //        return dt.ToShortTimeString();
                //} else {
                    return Value.ToString();
                //}
            }
            set { }
        }
 
        // References are the parsed tokens used in the Expr
        public HashSet<string> References { get; set; } = new HashSet<string>();

        // Resolved are the Symbol's that have been found (from the References)
        public Dictionary<string, Symbol> Resolved = new Dictionary<string, Symbol>();

        // Parameters are NCalc Parameters used in the call to NCalc.Evaluate()
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();


        private string definition = "";
        [JsonProperty]
        public virtual string Definition {
            get => definition;
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
                    Parameters.Clear();
                    Resolved.Clear();
                    References.Clear();
                    Error = null;
                    return;
                }
                Double result;

                if (value != definition && IsExpression) {
                    // The value has changed.  Clear what we had...cle
                    foreach (var symKvp in Resolved) {
                        Symbol s = symKvp.Value;
                        if (s != null) {
                            symKvp.Value.RemoveConsumer(this);
                        }
                    }
                    Resolved.Clear();
                    Parameters.Clear();
                }

                definition = value;

                if (Double.TryParse(value, out result)) {
                    Error = null;
                    IsExpression = false;
                    Value = result;
                    // Notify consumers
                    if (Symbol != null) {
                        Symbol.SymbolDirty(Symbol);
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
                    References.Clear();

                    foreach (var p in e.GetParameterNames()) {
                        References.Add(p);
                    }

                    // References now holds all of the CV's used in the expression
                    Parameters.Clear();
                    Resolved.Clear();
                    Evaluate();
                    if (Symbol != null) {
                        Symbol.SymbolDirty(Symbol);
                        Symbol.Validate();
                    }
                }
                RaisePropertyChanged("Definition");
                RaisePropertyChanged("Value");
                RaisePropertyChanged("ValueString");
                RaisePropertyChanged("IsAnnotated");
            }
        }
        public void RemoveParameter(string identifier) {
            Parameters.Remove(identifier);
            Resolved.Remove(identifier);
            Evaluate();
        }

        public void ReferenceRemoved(Symbol sym) {
            // A definition we use was removed
            string identifier = sym.Identifier;
            Parameters.Remove(identifier);
            Resolved.Remove(identifier);
            Evaluate();
        }
        private void AddParameter(string reference, object value) {
            Parameters.Add(reference, value);
        }
        private void Resolve(string reference, Symbol sym) {
            Parameters.Remove(reference);
            Resolved.Remove(reference);
            if (sym.Expr.Error == null) {
                Resolved.Add(reference, sym);
                //if (sym.Expr.Value == double.NegativeInfinity) {
                //    AddParameter(reference, sym.Expr.StringValue);
                //} else
                if (!Double.IsNaN(sym.Expr.Value)) {
                    AddParameter(reference, sym.Expr.Value);
                }
            }
        }
        public void Refresh() {
            Parameters.Clear();
            Resolved.Clear();
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
            if (Monitor.TryEnter(SYMBOL_LOCK, 1000)) {
                try {
                    if (!IsExpression) {
                        //Error = null;
                        return;
                    }
                    if (Definition.Length == 0) {
                        IsExpression = false;
                        RaisePropertyChanged("Value");
                        RaisePropertyChanged("ValueString");
                        //RaisePropertyChanged("StringValue");
                        RaisePropertyChanged("IsExpression");
                        return;
                    }
                    if (Context == null) return;
                    if (!Symbol.IsAttachedToRoot(Context)) {
                        return;
                    }

                    Dictionary<string, object> DataSymbols = SymbolBrokerVM.GetEquipmentKeys();

                    //if (Volatile || GlobalVolatile) {
                    //    IList<string> volatiles = new List<string>();
                    //    foreach (KeyValuePair<string, Symbol> kvp in Resolved) {
                    //        if (kvp.Value == null || kvp.Value.Expr.GlobalVolatile) {
                    //            volatiles.Add(kvp.Key);
                    //        }
                    //    }
                    //    foreach (string key in volatiles) {
                    //        Resolved.Remove(key);
                    //        Parameters.Remove(key);
                    //    }
                    //}

                    //Volatile = GlobalVolatile;

                    //ImageVolatile = false;

                    //StringValue = null;

                    if (Parameters.Count < Resolved.Count) {
                        Parameters.Clear();
                        Resolved.Clear();
                    }

                    // External, don't report error during validation
                    bool ext = false;

                    // First, validate References
                    foreach (string sRef in References) {
                        Symbol sym;
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
                            } else {
                                SymbolDictionary cached;
                                found = false;
                                // Try in the old Switch/Weather keys
                                object Val;
                                if (!found && DataSymbols.TryGetValue(symReference, out Val)) {
                                    // We don't want these resolved, just added to Parameters
                                    Resolved.Remove(symReference);
                                    Resolved.Add(symReference, null);
                                    Parameters.Remove(symReference);
                                    AddParameter(symReference, Val);
                                    //Volatile = true;
                                }
                            }
                        }
                    }

                    NCalc.Expression e = new NCalc.Expression(Definition, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
                    e.EvaluateFunction += ExtensionFunction;
                    e.Parameters = Parameters;

                    if (e.HasErrors()) {
                        Error = "Syntax Error";
                        return;
                    }

                    Error = null;
                    try {
                        if (Parameters.Count != References.Count) {
                            foreach (string r in References) {
                                string symReference = r;
                                if (symReference.StartsWith('_') || symReference.StartsWith('@')) {
                                    symReference = symReference.Substring(1);
                                }
                            }
                            RaisePropertyChanged("Error");
                            RaisePropertyChanged("ValueString");
                            RaisePropertyChanged("StringValue");
                            RaisePropertyChanged("Value");
                        } else {
                            Error = null;
                            object eval = e.Evaluate();
                            // We got an actual value
                            if (eval is Boolean b) {
                                Value = b ? 1 : 0;
                            } else {
                                try {
                                    Value = Convert.ToDouble(eval);
                                } catch (Exception) {
                                    //string str = (string)eval;
                                    //StringValue = str;
                                    //Value = double.NegativeInfinity;
                                    //if ("Integer".Equals(Type)) {
                                    //    Error = "Syntax error";
                                    //}
                                }
                            }
                            RaisePropertyChanged("Error");
                            //RaisePropertyChanged("StringValue");
                            RaisePropertyChanged("ValueString");
                            RaisePropertyChanged("Value");
                        }

                    } catch (NCalc.Exceptions.NCalcParameterNotDefinedException ex) {
                        Error = "Undefined: " + ex.ParameterName;
                    } catch (Exception ex) {
                        if (ex is NCalc.Exceptions.NCalcEvaluationException || ex is NCalc.Exceptions.NCalcParserException) {
                            Error = "Syntax Error";
                            return;
                        } else {
                            Error = "Unknown Error; see log";
                            Logger.Warning("Exception evaluating " + Definition + ": " + ex.Message);
                        }
                    }
                    Dirty = false;
                } finally {
                    Monitor.Exit(SYMBOL_LOCK);
                }
            } else {
                Logger.Error("Evaluate could not get SYMBOL_LOCK: " + this);
                //if (!LOCK_ERROR) {
                //    Notification.ShowError("Evaluate could not get SYMBOL_LOCK; see log for info");
                //}
                //LOCK_ERROR = true;
            }
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
                } else if (name == "dateTime") {
                    args.Result = 0;
                } else if (name == "CtoF") {
                    args.Result = 32 + (Convert.ToDouble(args.Parameters[0].Evaluate()) * 9 / 5);
                } else if (name == "MStoMPH") {
                    args.Result = (Convert.ToDouble(args.Parameters[0].Evaluate()) * 2.237);
                } else if (name == "KPHtoMPH") {
                    args.Result = (Convert.ToDouble(args.Parameters[0].Evaluate()) * .621);
                } else if (name == "dateString") {
                    if (args.Parameters.Length < 2) {
                        throw new ArgumentException();
                    }
                    args.Result = dt.ToString((string)args.Parameters[1].Evaluate());
                } else if (name == "defined") {
                    string str = Convert.ToString(args.Parameters[0].Evaluate());
                    args.Result = FindSymbol(str, Context.Parent) != null;
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
                return "Undefined";
            }
            return $"Expression: {Definition} in {id}, References: {References.Count}, Value: {ValueString}";
        }

    }
}
