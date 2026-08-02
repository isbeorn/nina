using NINA.Core.Locale;
using NINA.Core.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NINA.Sequencer.Logic.SymbolFunctions {

    public class TimeFunctions : IEnumerable<SymbolFunction> {
        private static DateTime GetDateTime(ISymbolFunctionArguments args) {
            DateTime dt;
            if (args.Count > 0) {
                try {
                    var utc = CoreUtil.UnixTimeStampToDateTime(
                        Convert.ToDouble(args.Evaluate(0), CultureInfo.InvariantCulture));
                    dt = utc.ToLocalTime();
                } catch {
                    dt = DateTime.MinValue;
                }
            } else {
                dt = DateTime.Now;
            }
            return dt;
        }

        private readonly ISymbolBroker symbolBroker;
        private readonly List<SymbolFunction> _all;

        public TimeFunctions(ISymbolBroker symbolBroker) {
            this.symbolBroker = symbolBroker;
            _all = [
                new SymbolFunction(
                    key: "Now",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_Now_Description"],
                    usageExample: "Now()",
                    implementation: args => CoreUtil.UnixTimeStampNow(),
                    minArgs: 0,
                    maxArgs: 0,
                    isVolatile: true
                ),

                new SymbolFunction(
                    key: "Hour",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_Hour_Description"],
                    usageExample: "Hour() or Hour(someDate)",
                    implementation: args => (int)GetDateTime(args).Hour,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Minute",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_Minute_Description"],
                    usageExample: "Minute() or Minute(someDate)",
                    implementation: args => (int)GetDateTime(args).Minute,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Day",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_Day_Description"],
                    usageExample: "Day() or Day(someDate)",
                    implementation: args => (int)GetDateTime(args).Day,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Month",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_Month_Description"],
                    usageExample: "Month() or Month(someDate)",
                    implementation: args => (int)GetDateTime(args).Month,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Year",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_Year_Description"],
                    usageExample: "Year() or Year(someDate)",
                    implementation: args => (int)GetDateTime(args).Year,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Dow",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_Dow_Description"],
                    usageExample: "Dow() or Dow(someDate)",
                    implementation: args => (int)GetDateTime(args).DayOfWeek,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    key: "AddMinutes",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_AddMinutes_Description"],
                    usageExample: "AddMinutes(now(), 30)",
                    implementation: args => {
                        DateTime baseDt;
                        double minutes;

                        if (args.Count == 1) {
                            // base: now, arg: minutes
                            baseDt = DateTime.Now;
                            minutes = Convert.ToDouble(args.Evaluate(0), CultureInfo.InvariantCulture);
                        } else {
                            // first arg: unix timestamp, second: minutes
                            var baseSeconds = Convert.ToDouble(args.Evaluate(0), CultureInfo.InvariantCulture);
                            baseDt = CoreUtil.UnixTimeStampToDateTime(baseSeconds).ToLocalTime();
                            minutes = Convert.ToDouble(args.Evaluate(1), CultureInfo.InvariantCulture);
                        }

                        var result = baseDt.AddMinutes(minutes);
                        return CoreUtil.ToUnixSeconds(result);
                    },
                    minArgs: 1,
                    maxArgs: 2,
                    isVolatile: true // can depend on current time when called with a single argument
                ),

                new SymbolFunction(
                    key: "AddHours",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_AddHours_Description"],
                    usageExample: "AddHours(now(), 1.5)",
                    implementation: args => {
                        DateTime baseDt;
                        double hours;

                        if (args.Count == 1) {
                            // base: now, arg: hours
                            baseDt = DateTime.Now;
                            hours = Convert.ToDouble(args.Evaluate(0), CultureInfo.InvariantCulture);
                        } else {
                            // first arg: unix timestamp, second: hours
                            var baseSeconds = Convert.ToDouble(args.Evaluate(0), CultureInfo.InvariantCulture);
                            baseDt = CoreUtil.UnixTimeStampToDateTime(baseSeconds).ToLocalTime();
                            hours = Convert.ToDouble(args.Evaluate(1), CultureInfo.InvariantCulture);
                        }

                        var result = baseDt.AddHours(hours);
                        return CoreUtil.ToUnixSeconds(result);
                    },
                    minArgs: 1,
                    maxArgs: 2,
                    isVolatile: true
                ),

                new SymbolFunction(
                    key: "SecondsSince",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_SecondsSince_Description"],
                    usageExample: "SecondsSince(lastEventTime) > 600",
                    implementation: args => {
                        var dt = GetDateTime(args); // arg[0] interpreted as unix seconds
                        var now = DateTime.Now;
                        return (now - dt).TotalSeconds;
                    },
                    minArgs: 1,
                    maxArgs: 1,
                    isVolatile: true
                ),

                new SymbolFunction(
                    key: "DateString",
                    category: "Time",
                    description: Loc.Instance["Lbl_SymbolFunction_Time_DateString_Description"],
                    usageExample: "DateString(now(), \"yyyy-MM-dd HH:mm:ss\")",
                    implementation: args => {
                        var dt = GetDateTime(args);
                        var fmt = Convert.ToString(args.Evaluate(1), CultureInfo.InvariantCulture);
                        return dt.ToString(fmt);
                    },
                    minArgs: 2,
                    maxArgs: 2
                )
            ];
        }

        public IEnumerator<SymbolFunction> GetEnumerator() => _all.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}