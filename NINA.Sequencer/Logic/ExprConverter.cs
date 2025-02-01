#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace NINA.Sequencer.Logic {

    public class ExprConverter : IMultiValueConverter {

        public static Dictionary<ISequenceEntity, bool> ValidityCache = new Dictionary<ISequenceEntity, bool>();

        public static string NOT_DEFINED = "Parameter was not defined (Parameter";

        private const int VALUE_EXP = 0;              // The expression to be evaluated
        private const int VALUE_STRING_VALUE = 1;          // If present, a validation method (range check, etc.)
        private const int VALUE_COMBO = 2;             // If present, a IList<string> of combo box values

        private long NowInSeconds = 0;
        private long NowPlusOneYear;
        private long NowMinusOneYear;

        private const int ONE_YEAR = 365 * 24 * 60 * 60;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            Expression expr = values[VALUE_EXP] as Expression;

            if (NowInSeconds == 0) {
                NowInSeconds = DateTimeOffset.Now.ToUnixTimeSeconds();
                NowPlusOneYear = NowInSeconds + ONE_YEAR;
                NowMinusOneYear = NowInSeconds - ONE_YEAR;
            }

            try {
                if (expr != null) {
                    if (expr.Error != null) {
                        return ""; // "{" + expr.Error + "}";
                    } else if (!expr.IsExpression) {
                        return "{Not an Expression}";
                    }
                    string txt;
                    if (expr.Error == null) {
                        if (false) { //expr.Context is ITrueFalse) {
                            if (expr.Value == 0) {
                                txt = "False";
                            } else {
                                txt = "True";
                            }
                        } else {
                            if (expr.Value == double.NegativeInfinity) {
                                txt = expr.ValueString;
                            } else if (expr.Value > NowMinusOneYear && expr.Value < NowPlusOneYear) {
                                // Handle dates
                                txt = expr.ValueString;
                            } else if (expr.Value == null) {
                                txt = "-null-";
                            } else {
                                txt = Math.Round((Double)expr.Value, 2).ToString();
                                if (values.Length > 2 && values[VALUE_COMBO] != null) {
                                    IList<string> combo = (IList<string>)values[VALUE_COMBO];
                                    int i = (int)expr.Value;
                                    if (i >= 0 && i < combo.Count) {
                                        txt = combo[i];
                                    }
                                }
                            }
                        
                        }
                        //                } else if (Double.IsNaN(expr.Value)) {
                        //                    txt = "Not evaluated";
                    } else {
                        txt = expr.Error;
                    }
                    return "{" + txt + "}";

                } else {
                    return "{??}";
                }
            } catch (Exception ex) {
                Logger.Error("ExprConverter: " + ex.Message);
                Logger.Error(ex.StackTrace);
                return "{Exception}";
            }
        }

 
        object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}