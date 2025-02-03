using Newtonsoft.Json;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NCalc;
using Google.Protobuf.WellKnownTypes;
using System.Windows.Media;
using static NINA.Sequencer.Logic.Symbol;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Windows;

namespace NINA.Sequencer.Logic {
    [JsonObject(MemberSerialization.OptIn)]
    public class Expression : BaseINPC {

        public Expression(string definition, ISequenceEntity context) {
            Definition = definition;
            Context = context;
        }

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

        public Action<Expression> Validator;

        public double? Default { get; set; }
        public double[]? Range { get; set; }
        public bool IsExpression { get; set; } = false;
        public bool IsSyntaxError { get; set; } = false;
        public bool IsAnnotated {
            get => IsExpression || Error != null;
            set { }
        }
        private double? _value = null;
        public virtual double? Value {
            get {
                if (_value == null && Default != null) {
                    return Default;
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
                    } else if (Validator != null) {
                        Validator(this);
                    }
                    //RaisePropertyChanged("StringValue");
                    RaisePropertyChanged("ValueString");
                    RaisePropertyChanged("IsExpression");
                    ////RaisePropertyChanged("DockableValue");
                }
            }
        }

        // 0x:- (greater than 0) ; 0:360 ; 0:360x ; -90:90 ; 0:59
        // split by -; check & remove x

        private void CheckRange(double value, double[] range) {
            int r = Convert.ToInt32(Range[2]);
            double min = Range[0] + (((r & 1) == 1) ? 1e-8 : 0);
            double max = Range[1] - (((r & 2) == 2) ? 1e-8 : 0);
            if (value <= min || (max != 0 && value >= max)) {
                if (r == 0) {
                    if (max == 0) {
                        Error = "Value must be " + min + " or greater";
                    } else {
                        Error = "Value must be between " + min + " and " + max;
                    }
                } else {
                    Error = "Value must be " + (((r & 1) == 1) ? "greater than " : "between ") + Range[0] + " and " + (((r & 2) == 2) ? "less than " : "") + Range[1];
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
                if (Error == null) return 24;
                return 18;
            }
            set { }
        }
        public string InfoButtonMargin {
            get {
                if (Error == null) return "5,0,0,0";
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
            } else if (Value == null && Definition.Length > 0) {
                Error = "Not evaluated";
            } else if (Definition.Length != 0 && Value == Default && Error == null) {
                // This seems very wrong to me; need to figure it out
                Evaluate(true);
            }
        }

        public void Validate() {
            Validate(null);
        }

        public static void AddExprIssues(IList<string> issues, params Expression[] exprs) {
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


        private string definition;
        [JsonProperty]
        public virtual string Definition {
            get => definition;
            set {
                if (value == null) return;
                value = value.Trim();
                if (value.Length == 0) {
                    IsExpression = false;
                    if (Default != null) {
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

                if (value.StartsWith('%') && value.EndsWith('%') && value.Length > 2) {
                    value = "__ENV_" + value.Substring(1, value.Length - 2);
                }

                if (value.StartsWith("~~")) {
                    Symbol.DumpSymbols();
                    value = value.Substring(2);
                }

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
                    // *****
                    //foreach (var p in e.GetParametersNames()) {
                    //    References.Add(p);
                    //}

                    // References now holds all of the CV's used in the expression
                    Parameters.Clear();
                    Resolved.Clear();
                    Evaluate();
                    if (Symbol != null) Symbol.SymbolDirty(Symbol);
                }
                RaisePropertyChanged("Expression");
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
                if (sym.Expr.Value != null) {
                    AddParameter(reference, sym.Expr.Value);
                }
            }
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

        public static string NOT_DEFINED = "Parameter was not defined (Parameter";

        public void Evaluate(bool validateOnly) {
            if (Monitor.TryEnter(SYMBOL_LOCK, 1000)) {
                try {
                    if (!IsExpression) {
                        //Error = null;
                        return;
                    }
                    if (Definition.Length == 0) {
                        // How the hell to clear the Expr
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
                    //Debug.WriteLine("Evaluate " + this);
                    Dictionary<string, object> DataSymbols = Symbol.GetSwitchWeatherKeys();

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
                        // Take care of "by reference" arguments
                        string symReference = sRef;
                        if (symReference.StartsWith("_") && !symReference.StartsWith("__")) {
                            symReference = sRef.Substring(1);
                        } else if (symReference.StartsWith("$")) {
                            symReference = symReference.Substring(1);
                            ext = true;
                        }
                        // Remember if we have any image data
                        //if (!ImageVolatile && symReference.StartsWith("Image_")) {
                        //    ImageVolatile = true;
                        //}
                        bool found = Resolved.TryGetValue(symReference, out sym);
                        if (!found || sym == null) {
                            // !found -> couldn't find it; sym == null -> it's a DataSymbol
                            if (!found) {
                                sym = Symbol.FindSymbol(symReference, Context.Parent);
                            }
                            if (sym != null) {
                                // Link Expression to the Symbol
                                Resolve(symReference, sym);
                                sym.AddConsumer(this);
                            } else {
                                SymbolDictionary cached;
                                found = false;
                                //if (SymbolCache.TryGetValue(WhenPluginObject.Globals, out cached)) {
                                //    Symbol global;
                                //    if (cached != null && cached.TryGetValue(symReference, out global)) {
                                //        Resolve(symReference, global);
                                //        global.AddConsumer(this);
                                //        found = true;
                                //    }
                                //}
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
                                if (!found && symReference.StartsWith("__ENV_")) {
                                    string env = Environment.GetEnvironmentVariable(symReference.Substring(6), EnvironmentVariableTarget.User);
                                    UInt32 val;
                                    if (env == null || !UInt32.TryParse(env, out val)) {
                                        val = 0;
                                    }
                                    // We don't want these resolved, just added to Parameters
                                    Resolved.Remove(symReference);
                                    Resolved.Add(symReference, null);
                                    Parameters.Remove(symReference);
                                    AddParameter(symReference, val);
                                    //Volatile = true;
                                }
                            }
                        }
                    }

                    NCalc.Expression e = new NCalc.Expression(Definition, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
                    //e.EvaluateFunction += ExtensionFunction;
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
                                if (!Parameters.ContainsKey(symReference)) {
                                    // Not defined or evaluated
                                    Symbol s = FindSymbol(symReference, Context.Parent);
                                    //if (s is SetVariable sv && !sv.Executed) {
                                    //    AddError("Not evaluated: " + r);
                                    //} else 
                                    if (r.StartsWith("_")) {
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
                            RaisePropertyChanged("Error");
                            RaisePropertyChanged("ValueString");
                            RaisePropertyChanged("StringValue");
                            RaisePropertyChanged("Value");
                        } else {
                            object eval = e.Evaluate();
                            // We got an actual value
                            if (eval is Boolean b) {
                                Value = b ? 1 : 0;
                                Error = null;
                            } else {
                                try {
                                    Value = Convert.ToDouble(eval);
                                    Error = null;
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
                            RaisePropertyChanged("StringValue");
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
    }
}
