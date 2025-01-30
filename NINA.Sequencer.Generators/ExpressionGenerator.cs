using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NINA.Sequencer.Generators {
    [Generator]
    public class ExpressionGenerator : IIncrementalGenerator {
        public void Initialize(IncrementalGeneratorInitializationContext context) {

            //Uncomment to attach a debugger for source generation
//#if DEBUG
//            if (!Debugger.IsAttached) {
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
            if(context.Node is not VariableDeclaratorSyntax && context.Node is not PropertyDeclarationSyntax) { return null; }

            var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);
            if (symbol == null) return null;

            // Look for [IsExpressionAttribute]
            var myPropAttr = symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "NINA.Sequencer.Generators.IsExpressionAttribute");

            if (myPropAttr == null) return null;

            // If found, we can also extract the ExtraInfo argument, if desired
            // (If you want to handle multiple arguments or advanced scenarios, adapt accordingly.)
            var extraInfo = (myPropAttr.ConstructorArguments.Length > 0)
                ? myPropAttr.ConstructorArguments[0].Value?.ToString() ?? ""
                : "";

            return new PropertyInfo(symbol.ContainingType, symbol, true);
        }

        private static string GeneratePartialClass(
        string namespaceName,
        string className,
        IGrouping<string, PropertyInfo?> properties
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

                cloneSource += $@"
                {propNameExpression} = {propNameExpression},
                {propName} = {propName},
                ";

                propertiesSource += $@"
        private Expression {fieldNameExpression};
        [JsonProperty]
        public Expression {propNameExpression} {{
            get => {fieldNameExpression};
            set {{
                {fieldNameExpression} = value;
                {propNameExpression}.Context = this;
                {propNameExpression}Setter(value);
                {propNameExpression}AfterSetter(value);
                RaisePropertyChanged();
            }}
        }}
                ";

                methodsSource += $@"
        public virtual void {propNameExpression}Setter(Expression exp) {{
            {propNameExpression}Validation(exp);
            if (exp != null && !exp.HasError) {{
                {propName} = exp.Value;
            }}
        }}
                    
        partial void {propNameExpression}Validation (Expression exp);
        partial void {propNameExpression}AfterSetter (Expression exp);
                ";
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
            var clone = new {className}(this) {{
                {cloneSource}
            }};
            AfterClone(clone);
            return clone;
        }}

        partial void AfterClone({className} clone);
{propertiesSource}
{methodsSource}
    }}
}}";
        }

        private sealed record PropertyInfo {
            public PropertyInfo(INamedTypeSymbol containingType, ISymbol propertySymbol, bool isDefinedByField) {
                ContainingType = containingType;
                PropertySymbol = propertySymbol;
                IsDefinedByField = isDefinedByField;
            }

            public INamedTypeSymbol ContainingType { get; }
            public ISymbol PropertySymbol { get; }
            public bool IsDefinedByField { get; }
        }
    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class IsExpressionAttribute : Attribute {
        public IsExpressionAttribute() {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExpressionObjectAttribute : Attribute {
        public ExpressionObjectAttribute() {
        }
    }
}
