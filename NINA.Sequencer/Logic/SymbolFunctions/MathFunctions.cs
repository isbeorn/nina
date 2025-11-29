using NINA.Astrometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NINA.Sequencer.Logic.SymbolFunctions {

    public class MathFunctions : IEnumerable<SymbolFunction> {
        private readonly ISymbolBroker symbolBroker;

        private readonly List<SymbolFunction> _all;

        public MathFunctions(ISymbolBroker symbolBroker) {
            this.symbolBroker = symbolBroker;
            _all = [
                new SymbolFunction(
                    name: "Abs",
                    category: "Math",
                    description: "Returns the absolute value of a specified number.",
                    usageExample: "Abs(-1)",
                    implementation: args => Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Acos",
                    category: "Math",
                    description: "Returns the angle whose cosine is the specified number.",
                    usageExample: "Acos(1)",
                    implementation: args => Math.Acos(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Asin",
                    category: "Math",
                    description: "Returns the angle whose sine is the specified number.",
                    usageExample: "Asin(0)",
                    implementation: args => Math.Asin(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Atan",
                    category: "Math",
                    description: "Returns the angle whose tangent is the specified number.",
                    usageExample: "Atan(0)",
                    implementation: args => Math.Atan(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Avg",
                    category: "Math",
                    description: "Returns the average (arithmetic mean) of all arguments.",
                    usageExample: "Avg(1, 2, 3)",
                    implementation: args => {
                        double sum = 0.0;
                        int count = args.Parameters.Length;
                        for (int i = 0; i < count; i++) {
                            sum += Convert.ToDouble(args.Parameters[i].Evaluate(), CultureInfo.InvariantCulture);
                        }
                        return sum / count;
                    },
                    minArgs: 1,
                    maxArgs: int.MaxValue
                ),

                new SymbolFunction(
                    name: "Ceiling",
                    category: "Math",
                    description: "Returns the smallest integer greater than or equal to the specified number.",
                    usageExample: "Ceiling(1.5)",
                    implementation: args => Math.Ceiling(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Cos",
                    category: "Math",
                    description: "Returns the cosine of the specified angle.",
                    usageExample: "Cos(0)",
                    implementation: args => Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Exp",
                    category: "Math",
                    description: "Returns e raised to the specified power.",
                    usageExample: "Exp(0)",
                    implementation: args => Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Floor",
                    category: "Math",
                    description: "Returns the largest integer less than or equal to the specified number.",
                    usageExample: "Floor(1.5)",
                    implementation: args => Math.Floor(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "IEEERemainder",
                    category: "Math",
                    description: "Returns the remainder resulting from the division of a specified number by another specified number.",
                    usageExample: "IEEERemainder(3, 2)",
                    implementation: args => Math.IEEERemainder(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2),

                new SymbolFunction(
                    name: "Ln",
                    category: "Math",
                    description: "Returns the natural logarithm of a specified number.",
                    usageExample: "Ln(1)",
                    implementation: args => Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Log",
                    category: "Math",
                    description: "Returns the logarithm of a specified number.",
                    usageExample: "Log(1, 10)",
                    implementation: args => Math.Log(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2),

                new SymbolFunction(
                    name: "Log10",
                    category: "Math",
                    description: "Returns the base 10 logarithm of a specified number.",
                    usageExample: "Log10(1)",
                    implementation: args => Math.Log10(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Max",
                    category: "Math",
                    description: "Returns the larger of two specified numbers.",
                    usageExample: "Max(1, 2)",
                    implementation: args => Math.Max(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2),

                new SymbolFunction(
                    name: "Min",
                    category: "Math",
                    description: "Returns the smaller of two numbers.",
                    usageExample: "Min(1, 2)",
                    implementation: args => Math.Min(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2),

                new SymbolFunction(
                    name: "Pow",
                    category: "Math",
                    description: "Returns a specified number raised to the specified power.",
                    usageExample: "Pow(3, 2)",
                    implementation: args => Math.Pow(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2),

                new SymbolFunction(
                    name: "Round",
                    category: "Math",
                    description: "Rounds a value to the nearest integer or specified number of decimal places.",
                    usageExample: "Round(3.222, 2)",
                    implementation: args => {
                        // 1 or 2 args: Round(x) or Round(x, decimals)
                        if (args.Parameters.Length == 2) {
                            return Math.Round(
                                Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                                Convert.ToInt32(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture));
                        }

                        return Math.Round(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    },
                    minArgs: 1, maxArgs: 2),

                new SymbolFunction(
                    name: "Sign",
                    category: "Math",
                    description: "Returns a value indicating the sign of a number.",
                    usageExample: "Sign(-10)",
                    implementation: args => Math.Sign(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Sin",
                    category: "Math",
                    description: "Returns the sine of the specified angle.",
                    usageExample: "Sin(0)",
                    implementation: args => Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Sqrt",
                    category: "Math",
                    description: "Returns the square root of a specified number.",
                    usageExample: "Sqrt(4)",
                    implementation: args => Math.Sqrt(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Tan",
                    category: "Math",
                    description: "Returns the tangent of the specified angle.",
                    usageExample: "Tan(0)",
                    implementation: args => Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Truncate",
                    category: "Math",
                    description: "Calculates the integral part of a number.",
                    usageExample: "Truncate(1.7)",
                    implementation: args => Math.Truncate(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1),

                new SymbolFunction(
                    name: "Mod",
                    category: "Math",
                    description: "Returns the mathematical modulus (remainder) of x / y. For positive y, the result is in [0, y).",
                    usageExample: "Mod(-1, 10)",
                    implementation: args => {
                        var a = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var b = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        var r = a % b;
                        if (r < 0 && b > 0) r += b;
                        return r;
                    },
                    minArgs: 2,
                    maxArgs: 2
                ),

                new SymbolFunction(
                    name: "Clamp",
                    category: "Math",
                    description: "Clamps a value to the inclusive range [min, max].",
                    usageExample: "Clamp(exposure, 1, 600)",
                    implementation: args => {
                        var v = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var lo = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        var hi = Convert.ToDouble(args.Parameters[2].Evaluate(), CultureInfo.InvariantCulture);
                        return Math.Clamp(v, lo, hi);
                    },
                    minArgs: 3,
                    maxArgs: 3
                ),

                new SymbolFunction(
                    name: "Between",
                    category: "Math",
                    description: "Returns whether x is between min and max (inclusive).",
                    usageExample: "Between(temperature, -10, 40)",
                    implementation: args => {
                        var v = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var lo = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        var hi = Convert.ToDouble(args.Parameters[2].Evaluate(), CultureInfo.InvariantCulture);
                        return v >= lo && v <= hi;
                    },
                    minArgs: 3,
                    maxArgs: 3
                ),

                new SymbolFunction(
                    name: "Deg",
                    category: "Math",
                    description: "Converts an angle from radians to degrees.",
                    usageExample: "Deg(PI() / 2)", // assuming user has a PI or similar
                    implementation: args => {
                        var rad = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return AstroUtil.ToDegree(rad);
                    },
                    minArgs: 1,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "Rad",
                    category: "Math",
                    description: "Converts an angle from degrees to radians.",
                    usageExample: "Rad(90)",
                    implementation: args => {
                        var deg = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return AstroUtil.ToRadians(deg);
                    },
                    minArgs: 1,
                    maxArgs: 1
                ),

                new SymbolFunction(
                    name: "Sum",
                    category: "Math",
                    description: "Returns the sum of all arguments.",
                    usageExample: "Sum(1, 2, 3)",
                    implementation: args => {
                        double sum = 0.0;
                        for (int i = 0; i < args.Parameters.Length; i++) {
                            sum += Convert.ToDouble(args.Parameters[i].Evaluate(), CultureInfo.InvariantCulture);
                        }
                        return sum;
                    },
                    minArgs: 1,
                    maxArgs: int.MaxValue
                )
            ];
        }

        public IEnumerator<SymbolFunction> GetEnumerator() => _all.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}