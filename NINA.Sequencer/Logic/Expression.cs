using Microsoft.CodeAnalysis.CSharp.Syntax;
using NCalc;
using NCalc.Exceptions;
using NCalc.Handlers;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.ColorSchema;
using NINA.Profile.Interfaces;
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

        //private static IProfileService ProfileService = null;

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
            //if (ProfileService == null) {
            //    ProfileService = (IProfileService)System.Windows.Application.Current.Resources["ProfileService"];
            //}
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

        public ISymbolBroker SymbolBroker { get; set; }

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
                            Error = Loc.Instance["LblMustBeInteger"];
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
                        Error = Loc.Instance["LblRange"] + ": >= " + min;
                    } else {
                        Error = Loc.Instance["LblRange"] +  ":" + min + " < " + Loc.Instance["LblValue"] + " < " + max;
                    }
                } else {
                    Error = Loc.Instance["ValueMustBe"] + " " + (((r & 1) == 1) ? ">" : Loc.Instance["LblBetween"]) + " " + Range[0] + " " + Loc.Instance["LblAnd"] + " <" + " " + (((r & 2) == 2) ? " < " : " <=" + " ") + Range[1];
                }
            }
        }
        
        public SolidColorBrush InfoButtonColor {
            get {
                if (Error == null) return new SolidColorBrush(Colors.White);
                return JustWarnings(Error) ?
                    // Don't like existing notification colors - they are hard to see.  Maybe add new ones to profiles?
                    new SolidColorBrush(Colors.Orange) :
                    new SolidColorBrush(Colors.Red);
                    //new SolidColorBrush(ProfileService.ActiveProfile.ColorSchemaSettings.ColorSchema.NotificationWarningColor) : 
                    //new SolidColorBrush(ProfileService.ActiveProfile.ColorSchemaSettings.ColorSchema.NotificationErrorColor);
            }
            set { }
        }

        public static bool JustWarnings(string error) {
            string[] errors = error.Split(";");
            bool red = false;
            bool orange = false;
            foreach (string e in errors) {
                // Note "External" not used currently
                if (e.Contains(Loc.Instance["LblNotEvaluated"]) || e.Contains("External")) {
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
                    return Loc.Instance["NoErrors"];
                } else if (JustWarnings(Error)) {
                    return string.Format(Loc.Instance["LblWarnings"], Error);
                } else {
                    return string.Format(Loc.Instance["LblErrors"], Error);
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
                        // We should expect this, since we're just trying to find the parameters used
                        Error = Loc.Instance["LblSyntaxError"];
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

        public void Evaluate(bool ignoreRoot) {
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
                    } else {
                        Logger.Warning("SymbolBroker not found in " + Context.Name);
                    }
                }
            }

            NCalc.Expression e = new NCalc.Expression(Definition, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
            e.EvaluateFunction += ExtensionFunction;
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
                                AddError(Loc.Instance["LblNotEvaluated"] + r);
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
                Error = Loc.Instance["LblError"] + ": " + ex.Message; // "Unknown Error; see log";
                Logger.Warning("Exception evaluating " + Definition + ": " + ex.Message);
            }
            Dirty = false;
        }

        private void ExtensionFunction(string name, FunctionArgs args) {
            try {
                if (SymbolBroker.TryInvokeFunction(name, args, out var result, out var isVolatile)) {
                    args.Result = result;

                    if (isVolatile) {
                        // Always check again on validation
                        GlobalVolatile = true;
                    }
                }
            } catch (Exception ex) {
                Logger.Error($"Error evaluating function {name}: {ex.Message}");
                throw new NCalcEvaluationException(ex.Message);
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
