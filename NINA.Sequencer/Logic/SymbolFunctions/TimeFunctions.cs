using NCalc.Handlers;
using NINA.Core.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NINA.Sequencer.Logic.SymbolFunctions {

    public class TimeFunctions : IEnumerable<SymbolFunction> {
        private static DateTime GetDateTime(FunctionArgs args) {
            DateTime dt;
            if (args.Parameters?.Length > 0) {
                try {
                    var utc = CoreUtil.UnixTimeStampToDateTime(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture));
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
                    name: "Now",
                    category: "Time",
                    description: "Returns the current Unix timestamp in seconds.",
                    usageExample: "Now()",
                    implementation: args => CoreUtil.UnixTimeStampNow(),
                    minArgs: 0,
                    maxArgs: 0,
                    isVolatile: true
                ),

                new SymbolFunction(
                    name: "Hour",
                    category: "Time",
                    description: "Returns the hour component (0–23) of a given datetime, or of the current time if no argument is supplied.",
                    usageExample: "Hour() or Hour(someDate)",
                    implementation: args => (int)GetDateTime(args).Hour,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "Minute",
                    category: "Time",
                    description: "Returns the minute component (0–59) of a given datetime, or of the current time if no argument is supplied.",
                    usageExample: "Minute() or Minute(someDate)",
                    implementation: args => (int)GetDateTime(args).Minute,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "Day",
                    category: "Time",
                    description: "Returns the day of the month (1–31) of a given datetime, or of the current date if no argument is supplied.",
                    usageExample: "Day() or Day(someDate)",
                    implementation: args => (int)GetDateTime(args).Day,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "Month",
                    category: "Time",
                    description: "Returns the month (1–12) of a given datetime, or of the current date if no argument is supplied.",
                    usageExample: "Month() or Month(someDate)",
                    implementation: args => (int)GetDateTime(args).Month,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "Year",
                    category: "Time",
                    description: "Returns the year component of a given datetime, or of the current date if no argument is supplied.",
                    usageExample: "Year() or Year(someDate)",
                    implementation: args => (int)GetDateTime(args).Year,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "Dow",
                    category: "Time",
                    description: "Returns the day of the week as an integer (0 = Sunday, 1 = Monday, … 6 = Saturday).",
                    usageExample: "Dow() or Dow(someDate)",
                    implementation: args => (int)GetDateTime(args).DayOfWeek,
                    minArgs: 0,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "AddMinutes",
                    category: "Time",
                    description: "Adds minutes to a datetime. Returns a Unix timestamp in seconds.",
                    usageExample: "AddMinutes(now(), 30)",
                    implementation: args => {
                        DateTime baseDt;
                        double minutes;

                        if (args.Parameters.Length == 1) {
                            // base: now, arg: minutes
                            baseDt = DateTime.Now;
                            minutes = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        } else {
                            // first arg: unix timestamp, second: minutes
                            var baseSeconds = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                            baseDt = CoreUtil.UnixTimeStampToDateTime(baseSeconds).ToLocalTime();
                            minutes = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        }

                        var result = baseDt.AddMinutes(minutes);
                        return CoreUtil.ToUnixSeconds(result);
                    },
                    minArgs: 1,
                    maxArgs: 2,
                    isVolatile: true // can depend on current time when called with a single argument
                ),

                new SymbolFunction(
                    name: "AddHours",
                    category: "Time",
                    description: "Adds hours to a datetime. Returns a Unix timestamp in seconds.",
                    usageExample: "AddHours(now(), 1.5)",
                    implementation: args => {
                        DateTime baseDt;
                        double hours;

                        if (args.Parameters.Length == 1) {
                            // base: now, arg: hours
                            baseDt = DateTime.Now;
                            hours = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        } else {
                            // first arg: unix timestamp, second: hours
                            var baseSeconds = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                            baseDt = CoreUtil.UnixTimeStampToDateTime(baseSeconds).ToLocalTime();
                            hours = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        }

                        var result = baseDt.AddHours(hours);
                        return CoreUtil.ToUnixSeconds(result);
                    },
                    minArgs: 1,
                    maxArgs: 2,
                    isVolatile: true
                ),

                new SymbolFunction(
                    name: "SecondsSince",
                    category: "Time",
                    description: "Returns the number of seconds elapsed since the given datetime (Unix timestamp, seconds).",
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
                    name: "DateString",
                    category: "Time",
                    description: "Formats a datetime value using the specified .NET format string.",
                    usageExample: "DateString(now(), \"yyyy-MM-dd HH:mm:ss\")",
                    implementation: args => {
                        var dt = GetDateTime(args);
                        var fmt = Convert.ToString(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
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