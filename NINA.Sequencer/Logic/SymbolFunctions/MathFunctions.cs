using NINA.Astrometry;
using NINA.Core.Locale;
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
                    key: "Abs",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Abs_Description"],
                    usageExample: "Abs(-1)",
                    implementation: args => Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Acos",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Acos_Description"],
                    usageExample: "Acos(1)",
                    implementation: args => Math.Acos(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Asin",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Asin_Description"],
                    usageExample: "Asin(0)",
                    implementation: args => Math.Asin(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Atan",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Atan_Description"],
                    usageExample: "Atan(0)",
                    implementation: args => Math.Atan(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Avg",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Avg_Description"],
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
                    key: "Ceiling",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Ceiling_Description"],
                    usageExample: "Ceiling(1.5)",
                    implementation: args => Math.Ceiling(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Cos",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Cos_Description"],
                    usageExample: "Cos(0)",
                    implementation: args => Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Exp",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Exp_Description"],
                    usageExample: "Exp(0)",
                    implementation: args => Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Floor",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Floor_Description"],
                    usageExample: "Floor(1.5)",
                    implementation: args => Math.Floor(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "IEEERemainder",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_IEEERemainder_Description"],
                    usageExample: "IEEERemainder(3, 2)",
                    implementation: args => Math.IEEERemainder(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2
                ),

                new SymbolFunction(
                    key: "Ln",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Ln_Description"],
                    usageExample: "Ln(1)",
                    implementation: args => Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Log",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Log_Description"],
                    usageExample: "Log(1, 10)",
                    implementation: args => Math.Log(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2
                ),

                new SymbolFunction(
                    key: "Log10",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Log10_Description"],
                    usageExample: "Log10(1)",
                    implementation: args => Math.Log10(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Max",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Max_Description"],
                    usageExample: "Max(1, 2)",
                    implementation: args => Math.Max(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2
                ),

                new SymbolFunction(
                    key: "Min",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Min_Description"],
                    usageExample: "Min(1, 2)",
                    implementation: args => Math.Min(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2
                ),

                new SymbolFunction(
                    key: "Pow",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Pow_Description"],
                    usageExample: "Pow(3, 2)",
                    implementation: args => Math.Pow(
                        Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 2, maxArgs: 2
                ),

                new SymbolFunction(
                    key: "Round",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Round_Description"],
                    usageExample: "Round(3.222, 2)",
                    implementation: args => {
                        if (args.Parameters.Length == 2) {
                            return Math.Round(
                                Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                                Convert.ToInt32(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture));
                        }
                        return Math.Round(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    },
                    minArgs: 1, maxArgs: 2
                ),

                new SymbolFunction(
                    key: "Sign",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Sign_Description"],
                    usageExample: "Sign(-10)",
                    implementation: args => Math.Sign(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Sin",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Sin_Description"],
                    usageExample: "Sin(0)",
                    implementation: args => Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Sqrt",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Sqrt_Description"],
                    usageExample: "Sqrt(4)",
                    implementation: args => Math.Sqrt(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Tan",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Tan_Description"],
                    usageExample: "Tan(0)",
                    implementation: args => Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Truncate",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Truncate_Description"],
                    usageExample: "Truncate(1.7)",
                    implementation: args => Math.Truncate(Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture)),
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Mod",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Mod_Description"],
                    usageExample: "Mod(-1, 10)",
                    implementation: args => {
                        var a = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var b = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        var r = a % b;
                        if (r < 0 && b > 0) r += b;
                        return r;
                    },
                    minArgs: 2, maxArgs: 2
                ),

                new SymbolFunction(
                    key: "Clamp",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Clamp_Description"],
                    usageExample: "Clamp(exposure, 1, 600)",
                    implementation: args => {
                        var v = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var lo = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        var hi = Convert.ToDouble(args.Parameters[2].Evaluate(), CultureInfo.InvariantCulture);
                        return Math.Clamp(v, lo, hi);
                    },
                    minArgs: 3, maxArgs: 3
                ),

                new SymbolFunction(
                    key: "Between",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Between_Description"],
                    usageExample: "Between(temperature, -10, 40)",
                    implementation: args => {
                        var v = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var lo = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        var hi = Convert.ToDouble(args.Parameters[2].Evaluate(), CultureInfo.InvariantCulture);
                        return v >= lo && v <= hi;
                    },
                    minArgs: 3, maxArgs: 3
                ),

                new SymbolFunction(
                    key: "Deg",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Deg_Description"],
                    usageExample: "Deg(PI() / 2)",
                    implementation: args => {
                        var rad = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return AstroUtil.ToDegree(rad);
                    },
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Rad",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Rad_Description"],
                    usageExample: "Rad(90)",
                    implementation: args => {
                        var deg = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        return AstroUtil.ToRadians(deg);
                    },
                    minArgs: 1, maxArgs: 1
                ),

                new SymbolFunction(
                    key: "Sum",
                    category: "Math",
                    description: Loc.Instance["Lbl_SymbolFunction_Math_Sum_Description"],
                    usageExample: "Sum(1, 2, 3)",
                    implementation: args => {
                        double sum = 0.0;
                        for (int i = 0; i < args.Parameters.Length; i++) {
                            sum += Convert.ToDouble(args.Parameters[i].Evaluate(), CultureInfo.InvariantCulture);
                        }
                        return sum;
                    },
                    minArgs: 1, maxArgs: int.MaxValue
                )

            ];
        }

        public IEnumerator<SymbolFunction> GetEnumerator() => _all.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}