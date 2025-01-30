using Newtonsoft.Json;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NCalc;
using Google.Protobuf.WellKnownTypes;
using System.Windows.Media;

namespace NINA.Sequencer.Logic {
    [JsonObject(MemberSerialization.OptIn)]
    public class Expression : BaseINPC {

        public Expression(string definition, ISequenceEntity context) {
            Definition = definition;
            Context = context;
        }

        public bool HasError => string.IsNullOrEmpty(Error);
        public string Error { get; set; }
        public double Value { get; set; }
        public bool Dirty { get; set; }
        public ISequenceEntity Context { get; set; }
        public double Default { get; set; } = Double.NaN;
        public bool IsExpression { get; set; } = false;
        public bool IsSyntaxError { get; set; } = false;
        public bool IsAnnotated {
            get => IsExpression || Error != null;
            set { }
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
                if (Error == null) return "5,-2,0,0";
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
                    //Evaluate();
                    if (Symbol != null) Symbol.SymbolDirty(Symbol);
                }
                RaisePropertyChanged("Expression");
                RaisePropertyChanged("IsAnnotated");
            }
        }
        public void RemoveParameter(string identifier) {
            Parameters.Remove(identifier);
            Resolved.Remove(identifier);
            //Evaluate();
        }

        public void ReferenceRemoved(Symbol sym) {
            // A definition we use was removed
            string identifier = sym.Identifier;
            Parameters.Remove(identifier);
            Resolved.Remove(identifier);
            //Evaluate();
        }
    }
}
