using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.Container;
using NINA.Core.Utility;
using System.Text.RegularExpressions;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Sequencer.Utility.DateTimeProvider;
using System.Linq;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.Expressions {
    [ExportMetadata("Name", "Set Variable to Date/Time")]
    [ExportMetadata("Description", "If the variable has been previously defined, its value will become the result of the specified expression")]
    [ExportMetadata("Icon", "VariableSVG")]
    [ExportMetadata("Category", "Expressions")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ResetVariableToDate : SequenceItem, IValidatable {

        private IList<IDateTimeProvider> dateTimeProviders;
        private IDateTimeProvider selectedProvider;
        private int hours;
        private int minutes;
        private int minutesOffset;
        private int seconds;

        [ImportingConstructor]
        public ResetVariableToDate(IList<IDateTimeProvider> dateTimeProviders) {
            Icon = Icon;
            Expr = new Expression("", Parent);
            DateTime = new SystemDateTime();
            DateTimeProviders = dateTimeProviders;
            SelectedProvider = DateTimeProviders?.FirstOrDefault();
        }

        public ResetVariableToDate(IList<IDateTimeProvider> dateTimeProviders, IDateTimeProvider selectedProvider) {
            DateTime = new SystemDateTime();
            DateTimeProviders = dateTimeProviders;
            SelectedProvider = selectedProvider;
        }

        public ResetVariableToDate(ResetVariableToDate copyMe) : this(copyMe.DateTimeProviders, copyMe.SelectedProvider) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
            }
        }

        public override object Clone() {
            ResetVariableToDate clone = new ResetVariableToDate(this) { };
            clone.Expr = new Expression(Expr.Definition, clone.Parent);
            clone.Variable = this.Variable;
            return clone;
        }

        private Expression _Expr = null;

        [JsonProperty]
        public Expression Expr {
            get => _Expr;
            set {
                _Expr = value;
                RaisePropertyChanged();
            }
        }

        private string variable;

        [JsonProperty]
        public string Variable {
            get => variable;
            set {
                if (value == variable) {
                    return;
                }
                variable = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }
  
        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Bunch of reasons the instructiopn might be invalid
            if (Issues.Count != 0) {
                throw new SequenceEntityFailedException("The instruction is invalid");
            }
            Expr.Evaluate();

            // Find Symbol, make sure it's valid
            UserSymbol sym = UserSymbol.FindSymbol(Variable, Parent);
            if (sym == null || !(sym is Variable) && !Expression.JustWarnings(Expr.Error)) {
                throw new SequenceEntityFailedException("The symbol isn't found or isn't a Variable");
            } else if (Expr.Error != null) {
                throw new SequenceEntityFailedException("The value of the expression '" + Expr.Definition + "' was invalid");
            }
            Variable sv = sym as Variable;
            if (sv == null || sv.Executed == false) {
                throw new SequenceEntityFailedException("The Variable definition has not been executed");
            }

            string oldDefinition = sym.Expr.Definition;

            if (Expr.StringValue != null) {
                sym.Expr.Error = null;
                sym.Expr.Definition = "'" + Expr.StringValue + "'";
            } else {
                sym.Expr.Definition = Expr.Value.ToString();
            }

            Logger.Info("SetVariableToDate: " + Variable + " from " + oldDefinition + " to " + sym.Expr.Definition);

            // Make sure references are updated
            UserSymbol.SymbolDirty(sym);

            return Task.CompletedTask;
        }

        private bool IsAttachedToRoot() {
            ISequenceContainer p = Parent;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return true;
                }
                p = p.Parent;
            }
            return false;
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            UpdateTime();
            //Expr.Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ResetVariable)}, Variable: {variable}, Expr: {Expr}";
        }

        public IList<IDateTimeProvider> DateTimeProviders {
            get => dateTimeProviders;
            set {
                dateTimeProviders = value;
                RaisePropertyChanged();
            }
        }

        public bool HasFixedTimeProvider => selectedProvider != null && !(selectedProvider is NINA.Sequencer.Utility.DateTimeProvider.TimeProvider);

        [JsonProperty]
        public IDateTimeProvider SelectedProvider {
            get => selectedProvider;
            set {
                selectedProvider = value;
                if (selectedProvider != null) {
                    UpdateTime();
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(HasFixedTimeProvider));
                }
            }
        }

        public string TimeString { get; set; } = "Not Set";

        private bool timeDeterminedSuccessfully;
        private DateTime lastReferenceDate;
        private void UpdateTime() {
            try {
                lastReferenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
                if (HasFixedTimeProvider) {
                    DateTime t = SelectedProvider.GetDateTime(this) + TimeSpan.FromMinutes(MinutesOffset);
                    Hours = t.Hour;
                    Minutes = t.Minute;
                    Seconds = t.Second;
                    Expr.Value = ((DateTimeOffset)t).ToUnixTimeSeconds();
                    TimeString = Expr.ValueString;
                    RaisePropertyChanged("Expr.Value");
                    RaisePropertyChanged("Expr.ValueString");
                    RaisePropertyChanged("Expr");
                    RaisePropertyChanged("TimeString");
                }
                timeDeterminedSuccessfully = true;
            } catch (Exception) {
                timeDeterminedSuccessfully = false;
                Validate();
            }
        }

        [JsonProperty]
        public int Hours {
            get => hours;
            set {
                hours = value;
                RaisePropertyChanged();
                Validate();
            }
        }

        [JsonProperty]
        public int Minutes {
            get => minutes;
            set {
                minutes = value;
                RaisePropertyChanged();
                Validate();
            }
        }

        [JsonProperty]
        public int MinutesOffset {
            get => minutesOffset;
            set {
                minutesOffset = value;
                UpdateTime();
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int Seconds {
            get => seconds;
            set {
                seconds = value;
                RaisePropertyChanged();
                Validate();
            }
        }

        public ICustomDateTime DateTime { get; set; }

        public bool Validate() {
            if (!IsAttachedToRoot()) return true;

            var i = new List<string>();
            if (Variable == null || Variable.Length == 0) {
                i.Add("The variable and new value expression must both be specified");
            } else if (Variable.Length > 0 && !Regex.IsMatch(Variable, UserSymbol.VALID_SYMBOL)) {
                i.Add("'" + Variable + "' is not a legal Variable name");
            } else {
                UserSymbol sym = UserSymbol.FindSymbol(Variable, Parent);
                if (sym == null) {
                    i.Add("The Variable '" + Variable + "' is not in scope.");
                } else if (sym is Constant) {
                    i.Add("The symbol '" + Variable + "' is a Constant and may not be used with this instruction");
                }
            }
            if (HasFixedTimeProvider) {
                var referenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
                if (lastReferenceDate != referenceDate) {
                    UpdateTime();
                }
            } else {
                DateTime today = System.DateTime.Today;
                today = today.AddHours(Hours);
                today = today.AddMinutes(Minutes);
                today = today.AddSeconds(Seconds);
                Expr.Value = ((DateTimeOffset)today).ToUnixTimeSeconds();
                TimeString = Expr.ValueString;
                //RaisePropertyChanged("Expr.Value");
                //RaisePropertyChanged("Expr.ValueString");
                RaisePropertyChanged("Expr");
                RaisePropertyChanged("TimeString");
            }

            if (!timeDeterminedSuccessfully) {
                i.Add(Loc.Instance["LblSelectedTimeSourceInvalid"]);
            }

            Expr.Evaluate();
            
            Issues = i;
            return Issues.Count == 0;
        }

    }
}
