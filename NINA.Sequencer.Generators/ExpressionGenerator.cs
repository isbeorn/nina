using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

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
                predicate: static (node, ct) => IsPropertyWithAttributes(node) || IsCandidateField(node),
                transform: static (ctx, ct) => GetFieldPropertyInfoOrNull(ctx)
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

                bool hasExpressionObject = classSymbol
                        .GetAttributes()
                        .Any(a => a.AttributeClass?.ToDisplayString() == "NINA.Sequencer.Generators.ExpressionObjectAttribute");

                // If the class is missing [ExpressionObject], emit a diagnostic and skip generating code
                if (!hasExpressionObject) {
                    // Create a diagnostic
                    var descriptor = new DiagnosticDescriptor(
                        id: "EXP0001",
                        title: "IsExpression usage error",
                        messageFormat: "Property '{0}' is marked with [IsExpression], but the containing class '{1}' is missing [ExpressionObject].",
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
                var generatedSource = GeneratePartialClass(ns, className, group);

                // Add the source using a stable hint name:
                var hintName = $"{className}_ExpressionAttribute.g.cs";
                context.AddSource(hintName, generatedSource);
            }
        }

        private static bool IsPropertyWithAttributes(SyntaxNode node) {
            return node is PropertyDeclarationSyntax pds && pds.AttributeLists.Count > 0;
        }
        static bool IsCandidateField(SyntaxNode node) {
            // The node must represent a field declaration
            if (node is not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax { AttributeLists.Count: > 0 } fieldNode } }) {

                return false;
            }


            return true;
        }

        private static PropertyInfo? GetFieldPropertyInfoOrNull(GeneratorSyntaxContext context) {
            // node must be PropertyDeclarationSyntax due to predicate
            if (context.Node is not VariableDeclaratorSyntax && context.Node is not PropertyDeclarationSyntax) { return null; }

            var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);
            if (symbol == null) return null;

            // Look for [IsExpressionAttribute]
            var myPropAttr = symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "NINA.Sequencer.Generators.IsExpressionAttribute");

            if (myPropAttr == null) return null;

            IEnumerable<KeyValuePair<string, TypedConstant>> args = myPropAttr.NamedArguments;

            // If found, we can also extract the ExtraInfo argument, if desired
            // (If you want to handle multiple arguments or advanced scenarios, adapt accordingly.)
            var extraInfo = (myPropAttr.ConstructorArguments.Length > 0)
                ? myPropAttr.ConstructorArguments[0].Value?.ToString() ?? ""
                : "";

            return new PropertyInfo(symbol.ContainingType, symbol, true, args);
        }

        private static string GeneratePartialClass(string namespaceName, string className, IGrouping<string, PropertyInfo?> properties
    ) {
            // Build the partial class with one method per property
            var cloneSource = string.Empty;
            var propertiesSource = string.Empty;
            var methodsSource = string.Empty;

            foreach (var prop in properties) {
                if (prop is null) continue;
                string propName = prop.PropertySymbol.Name;
                if (prop.IsDefinedByField) {
                    if (prop.PropertySymbol.Name.StartsWith("_")) {
                        propName = prop.PropertySymbol.Name.Substring(1, 2).ToUpper() + propName.Substring(2);
                    } else {
                        propName = prop.PropertySymbol.Name.Substring(0, 1).ToUpper() + propName.Substring(1);
                    }
                }
                string fieldName = propName.Substring(0, 1).ToLower() + propName.Substring(1);
                string fieldNameExpression = fieldName + "Expression";
                string propNameExpression = propName + "Expression";
                bool hasValidator = false;
                string? proxy = null;

                IFieldSymbol fieldSymbol = (IFieldSymbol)prop.PropertySymbol;
                string fieldType = fieldSymbol.Type.Name;
                if (fieldType == "Int32") fieldType = "int";

                cloneSource += $@"
                {propNameExpression} = new Expression ({propNameExpression}),";

                propertiesSource += $@"

        private Expression {fieldNameExpression} = new Expression(null, null);
        [JsonProperty]
        public Expression {propNameExpression} {{
            get => {fieldNameExpression};
            set {{
                {fieldNameExpression} = value;
                if (value == null) return;
                {propNameExpression}.Context = this;";

                foreach (KeyValuePair<string, TypedConstant> kvp in prop.Args) {

                    if (kvp.Key == "HasValidator") {
                        hasValidator = true;
                    } else if (kvp.Key == "Proxy") {
                        proxy = (string)kvp.Value.Value;
//                        propertiesSource += $@"
//                {propNameExpression}.Definition = {kvp.Value.Value}.ToString();";

                    } else if (kvp.Value.Type?.TypeKind == TypeKind.Array) {
                        var values = kvp.Value.Values;
                        double min = (double)values[0].Value;
                        double max = (double)values[1].Value;
                        double r = 0;
                        if (values.Length > 2) {
                            r = (double)values[2].Value;
                        }
                        propertiesSource += $@"
                {propNameExpression}.{kvp.Key} = new double[] {{{min}, {max}, {r}}};";
                    } else if (kvp.Key == "Default") {
                        propertiesSource += $@"
                {propNameExpression}.{kvp.Key} = {kvp.Value.Value};";
                    } else if (kvp.Key == "DefaultString") {
                        propertiesSource += $@"
                {propNameExpression}.{kvp.Key} = ""{kvp.Value.Value}"";";
                    }
                }

                if (hasValidator) {
                    propertiesSource += $@"
                {propNameExpression}.Validator = {propNameExpression}Validator;";
                }

                propertiesSource += $@"
                RaisePropertyChanged();
            }}
        }}";
                if (hasValidator) {
                    propertiesSource += $@"
        
        partial void {propNameExpression}Validator(Expression expr);";
                }

                if (proxy == null) {
                    propertiesSource += $@"

        public {fieldType} {propName} {{
            get {{
                return ({fieldType}){propNameExpression}.Value;
            }}
            set {{
                {propNameExpression}.Definition = value.ToString();
                RaisePropertyChanged();
            }}
        }}
";
                } else {
                    propertiesSource += $@"

        [JsonProperty]
        public {fieldType} {propName} {{
            get => {propNameExpression}.Definition;
            set {{
                {propNameExpression}.Definition = value;
                {proxy} = {propNameExpression}.Value;
            }}
        }}
";
                }

            }

            return $@"// <auto-generated />
using System;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Sequencer.Logic;

namespace {namespaceName}
{{
    partial class {className}
    {{
        public override object Clone() {{
            var clone = new {className}(this) {{{cloneSource}
            }};
            AfterClone(clone, this);
            return clone;
        }}

        partial void AfterClone({className} clone, {className} cloned);
{propertiesSource}
{methodsSource}
    }}
}}";
        }

        private sealed record PropertyInfo {
            public PropertyInfo(INamedTypeSymbol containingType, ISymbol propertySymbol, bool isDefinedByField, IEnumerable<KeyValuePair<string, TypedConstant>> args) {
                ContainingType = containingType;
                PropertySymbol = propertySymbol;
                IsDefinedByField = isDefinedByField;
                Args = args;
            }

            public INamedTypeSymbol ContainingType { get; }
            public ISymbol PropertySymbol { get; }
            public bool IsDefinedByField { get; }
            public IEnumerable<KeyValuePair<string, TypedConstant>> Args;
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
    public sealed class ExpressionObjectAttribute : Attribute {
        public ExpressionObjectAttribute() {
        }
    }
}
