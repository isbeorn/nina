#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace NINA.Sequencer.Generators {
    [Generator]
    public class ExpressionGenerator : IIncrementalGenerator {
        public void Initialize(IncrementalGeneratorInitializationContext context) {

            //Uncomment to attach a debugger for source generation
            //#if DEBUG
            //            if (!Debugger.IsAttached) {//
            //                Debugger.Launch();
            //            }
            //#endif 

            var propertyDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, ct) => IsCandidatePartialProperty(node),
                transform: static (ctx, ct) => GetPropertyInfoOrNull(ctx)
            ).Where(m => m is not null);

                    var allProperties = propertyDeclarations.Collect();
                    context.RegisterSourceOutput(allProperties, Execute);
        }

        private void Execute(SourceProductionContext context, ImmutableArray<PropertyInfo?> propertyInfos) {
            // Group properties by the full metadata name of their containing type
            var groupedByContainingType = propertyInfos
                .GroupBy(p => p!.ContainingType.ToDisplayString());

            foreach (var group in groupedByContainingType) {
                var propertySymbol = group.First();
                var classSymbol = propertySymbol.ContainingType;
                var className = classSymbol.Name;
                var ns = classSymbol.ContainingNamespace?.ToDisplayString() ?? "";
                string broker = null;

                bool hasUsesExpressions = classSymbol
                        .GetAttributes()
                        .Any(a => a.AttributeClass?.ToDisplayString() == "NINA.Sequencer.Generators.UsesExpressionsAttribute");

                foreach (var attribute in classSymbol.GetAttributes()) {
                    if (attribute.AttributeClass?.ToDisplayString() == "NINA.Sequencer.Generators.UsesExpressionsAttribute") {
                        if (attribute.ConstructorArguments.Length > 0) {
                            broker = (string)attribute.ConstructorArguments[0].Value;
                        }
                    }
                }

                // If the class is missing [UsesExpressions ("symbolBroker")], emit a diagnostic and skip generating code
                if (!hasUsesExpressions) {
                    // Create a diagnostic
                    var descriptor = new DiagnosticDescriptor(
                        id: "EXP0001",
                        title: "IsExpression usage error",
                        messageFormat: "Property '{0}' is marked with [IsExpression], but the containing class '{1}' is missing [UsesExpressions].",
                        category: "Usage",
                        DiagnosticSeverity.Hidden,
                        isEnabledByDefault: true);

                    var diag = Diagnostic.Create(
                        descriptor,
                        propertySymbol.PropertySymbol.Locations.FirstOrDefault(),
                        propertySymbol.PropertySymbol.Name,
                        classSymbol.Name);

                    context.ReportDiagnostic(diag);
                    // Do NOT generate code for this property
                    continue;
                }

                // Generate partial class code
                var generatedSource = GeneratePartialClass(ns, className, group, broker);

                // Add the source using a stable hint name:
                var hintName = $"{className}_ExpressionAttribute.g.cs";
                context.AddSource(hintName, generatedSource);
            }
        }

        private static bool IsPropertyWithAttributes(SyntaxNode node) {
            return node is PropertyDeclarationSyntax pds && pds.AttributeLists.Count > 0;
        }
        private static bool IsCandidatePartialProperty(SyntaxNode node) {
            if (node is not PropertyDeclarationSyntax pds)
                return false;

            if (pds.AttributeLists.Count == 0)
                return false;

            // Must be partial
            if (!pds.Modifiers.Any(SyntaxKind.PartialKeyword))
                return false;

            // Must be an auto-like signature (no bodies / expression bodies)
            if (pds.ExpressionBody is not null)
                return false;

            if (pds.AccessorList is null)
                return false;

            foreach (var acc in pds.AccessorList.Accessors) {
                // C# 12 partial property declaration uses semicolon accessors
                // e.g. get; set; (no bodies)
                if (acc.Body is not null || acc.ExpressionBody is not null)
                    return false;

                if (acc.SemicolonToken.IsKind(SyntaxKind.None))
                    return false;
            }

            return true;
        }

        private static PropertyInfo? GetPropertyInfoOrNull(GeneratorSyntaxContext context) {
            if (context.Node is not PropertyDeclarationSyntax)
                return null;

            var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node) as IPropertySymbol;
            if (symbol is null)
                return null;

            var myPropAttr = symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "NINA.Sequencer.Generators.IsExpressionAttribute");

            if (myPropAttr is null)
                return null;

            var args = myPropAttr.NamedArguments;

            var extraInfo = (myPropAttr.ConstructorArguments.Length > 0)
                ? myPropAttr.ConstructorArguments[0].Value?.ToString() ?? ""
                : "";

            return new PropertyInfo(symbol.ContainingType, symbol, args, extraInfo);
        }

        private static string GeneratePartialClass(string namespaceName, string className, IGrouping<string, PropertyInfo?> properties, string broker) {
            // Build the partial class with one method per property
            var cloneSource = string.Empty;
            var expressionClones = string.Empty;
            var propertiesSource = string.Empty;
            var methodsSource = string.Empty;

            if (broker != null) {
                cloneSource += $@"
                {broker} = {broker},";
            }

            foreach (var prop in properties) {
                if (prop is null) continue;
                var propSym = prop.PropertySymbol;
                string propName = propSym.Name;
                string fieldName = propName.Substring(0, 1).ToLower() + propName.Substring(1);
                string fieldNameExpression = fieldName + "Expression";
                string propNameExpression = propName + "Expression";
                bool hasValidator = false;
                string? proxy = null;
                bool jsonIgnore = false;
                bool hasDefault = false;

                IPropertySymbol fieldSymbol = (IPropertySymbol)prop.PropertySymbol;
                string fieldType = fieldSymbol.Type.Name;
                if (fieldType == "Int32") fieldType = "int";

                propertiesSource += $@"

        private Expression {fieldNameExpression};
        [JsonProperty (Order = -1)]
        public Expression {propNameExpression} {{
            get {{
                if ({fieldNameExpression} == null) {{
                    {fieldNameExpression} = new Expression(null, null);
                    {fieldNameExpression}.Context = this;
                    {fieldNameExpression}.Type = ""{fieldType}"";
                    {fieldNameExpression}.SymbolBroker = SymbolBroker;";
                foreach (KeyValuePair<string, TypedConstant> kvp in prop.Args) {

                    if (kvp.Key == "HasValidator") {
                        hasValidator = true;
                    } else if (kvp.Key == "Proxy") {
                        proxy = (string)kvp.Value.Value;
                        jsonIgnore = true;
                    } else if (kvp.Value.Type?.TypeKind == TypeKind.Array) {
                        var values = kvp.Value.Values;
                        double min = Convert.ToDouble(values[0].Value, CultureInfo.InvariantCulture);
                        double max = Convert.ToDouble(values[1].Value, CultureInfo.InvariantCulture);
                        double r = 0;
                        if (values.Length > 2) {
                            r = Convert.ToDouble(values[2].Value, CultureInfo.InvariantCulture);
                        }
                        propertiesSource += $@"
                    {fieldNameExpression}.{kvp.Key} = new double[] {{{min.ToString(CultureInfo.InvariantCulture)}, {max.ToString(CultureInfo.InvariantCulture)}, {r.ToString(CultureInfo.InvariantCulture)}}};";
                    } else if (kvp.Key == "Default") {
                        propertiesSource += $@"
                    {fieldNameExpression}.{kvp.Key} = {Convert.ToString(kvp.Value.Value, CultureInfo.InvariantCulture)};";
                        hasDefault = true;
                    } else if (kvp.Key == "DefaultString") {
                        propertiesSource += $@"
                    {fieldNameExpression}.{kvp.Key} = ""{kvp.Value.Value}"";";
                    }
                }

                if (hasValidator) {
                    propertiesSource += $@"
                    {fieldNameExpression}.Validator = {propNameExpression}Validator;";
                }

                expressionClones += $@"
            clone.{propNameExpression} = new Expression (this.{propNameExpression}, clone, {(hasValidator ? $"clone.{propNameExpression}Validator" : "null")});";

                propertiesSource += $@"
                }}
                return {fieldNameExpression};
            }}
            set {{
                {fieldNameExpression} = value;
                if (value == null) return;";
                propertiesSource += $@"
                RaisePropertyChanged();
            }}
        }}";
                if (hasValidator) {
                    propertiesSource += $@"
        
        partial void {propNameExpression}Validator(Expression expr);
";
                }


                if (proxy != null) {
                    propertiesSource += $@"

        [Json";
                    propertiesSource += jsonIgnore ? "Ignore" : "Property";
                    propertiesSource += $@"]
        public partial {fieldType} {propName} {{
            get => {proxy};
            set {{
                {propNameExpression}.Definition = Convert.ToString(value, CultureInfo.InvariantCulture);
                {proxy} = {propNameExpression}.Value;
            }}
        }}
";
                } else {
                    propertiesSource += $@"
        [JsonProperty (Order = 0)]
        public partial {fieldType} {propName} {{
            get {{ 
                {propNameExpression}.Evaluate(true); 
                return ({fieldType}) {propNameExpression}.";
                    if (fieldType == "String") {
                        propertiesSource += "Definition";
                    } else {
                        propertiesSource += "Value";
                    }
                    propertiesSource += $@";
            }}
            set {{
                {propNameExpression}.Definition = ";
                    if (fieldType == "String") {
                        propertiesSource += "value;";
                    } else {
                        propertiesSource += $@"(value == {propNameExpression}.Default && {propNameExpression}.DefaultString != null) ? String.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);";
                    }
                    propertiesSource += $@"
            }}
        }}

        [JsonProperty (Order = 1)]
        public string {propName}Definition {{
            get => {propNameExpression}.Definition;
            set {{
                {propNameExpression}.Definition = value;
            }}
        }}";
                }

            }

            return $@"// <auto-generated />
using System;
using System.Globalization;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Generators;

namespace {namespaceName}
{{
    partial class {className}
    {{
        public override object Clone() {{
            var clone = new {className}(this) {{{cloneSource}
            }};
            {expressionClones}
            AfterClone(this, clone);
            AfterClone(clone);
            return clone;
        }}

        partial void AfterClone({className} clone);
        partial void AfterClone({className} original, {className} clone);
{propertiesSource}
{methodsSource}
    }}
}}";
        }

        private sealed record PropertyInfo {
            public PropertyInfo(INamedTypeSymbol containingType, IPropertySymbol propertySymbol,
                IEnumerable<KeyValuePair<string, TypedConstant>> args, string broker) {
                ContainingType = containingType;
                PropertySymbol = propertySymbol;
                Args = args;
                Broker = broker;
            }

            public INamedTypeSymbol ContainingType { get; }
            public IPropertySymbol PropertySymbol { get; }
            public IEnumerable<KeyValuePair<string, TypedConstant>> Args;
            public string Broker;
        }
    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IsExpressionAttribute : Attribute {
        public IsExpressionAttribute() {
        }

        public double _def = 0;
        public double Default {
            get { return _def; }
            set { _def = value; }
        }

        public double[] _range = new double[3];
        public double[] Range {
            get { return _range; }
            set { _range = value; }
        }

        public string _defaultString = "";
        public string DefaultString {
            get { return _defaultString; }
            set { _defaultString = value; }
        }

        public bool _hasValidator = false;
        public bool HasValidator {
            get { return _hasValidator; }
            set { _hasValidator = value; }
        }

        public string _proxy = "";
        public string Proxy {
            get { return _proxy; }
            set { _proxy = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UsesExpressionsAttribute : Attribute {
        public UsesExpressionsAttribute() {
        }
    }
}
