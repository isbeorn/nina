#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Sequencer.Logic;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NINA.Test.Sequencer.Logic {

    [TestFixture]
    public class SymbolFunctionApiContractTest {

        [Test]
        public void PluginFacingSymbolApi_DoesNotExposeNCalcTypes() {
            Type[] apiTypes = [
                typeof(ISymbolFunctionArguments),
                typeof(ISymbolBroker),
                typeof(ISymbolProvider),
                typeof(SymbolFunction)
            ];

            IReadOnlyCollection<Type> exposedTypes = apiTypes
                .SelectMany(GetExposedTypes)
                .Distinct()
                .ToArray();

            exposedTypes.Where(IsNCalcType).Should().BeEmpty();
        }

        private static IEnumerable<Type> GetExposedTypes(Type apiType) {
            yield return apiType;

            foreach (ConstructorInfo constructor in apiType.GetConstructors()) {
                foreach (ParameterInfo parameter in constructor.GetParameters()) {
                    foreach (Type type in ExpandType(parameter.ParameterType)) {
                        yield return type;
                    }
                }
            }

            foreach (PropertyInfo property in apiType.GetProperties()) {
                foreach (Type type in ExpandType(property.PropertyType)) {
                    yield return type;
                }
            }

            foreach (MethodInfo method in apiType.GetMethods()) {
                foreach (Type type in ExpandType(method.ReturnType)) {
                    yield return type;
                }

                foreach (ParameterInfo parameter in method.GetParameters()) {
                    foreach (Type type in ExpandType(parameter.ParameterType)) {
                        yield return type;
                    }
                }
            }

            foreach (EventInfo eventInfo in apiType.GetEvents()) {
                if (eventInfo.EventHandlerType is Type eventHandlerType) {
                    foreach (Type type in ExpandType(eventHandlerType)) {
                        yield return type;
                    }
                }
            }
        }

        private static IEnumerable<Type> ExpandType(Type type) {
            yield return type;

            if (type.GetElementType() is Type elementType) {
                foreach (Type expandedType in ExpandType(elementType)) {
                    yield return expandedType;
                }
            }

            foreach (Type genericArgument in type.GetGenericArguments()) {
                foreach (Type expandedType in ExpandType(genericArgument)) {
                    yield return expandedType;
                }
            }
        }

        private static bool IsNCalcType(Type type) {
            return type.Namespace?.StartsWith("NCalc", StringComparison.Ordinal) == true
                || type.Assembly.GetName().Name?.StartsWith("NCalc", StringComparison.Ordinal) == true;
        }
    }
}
