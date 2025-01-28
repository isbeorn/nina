using Microsoft.CodeAnalysis;
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
//#if DEBUG
//            if (!Debugger.IsAttached) {
//                Debugger.Launch();
//            }
//#endif 
            var propertyDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, ct) => IsPropertyWithAttributes(node),
                transform: static (ctx, ct) => GetPropertyInfoOrNull(ctx)
            ).Where(m => m is not null);

            var allProperties = propertyDeclarations.Collect();

            context.RegisterSourceOutput(allProperties, Execute);
            /*
            var compilation = context.CompilationProvider.Combine(provider.Collect());

            context.RegisterSourceOutput(compilation, Execute);*/
        }

        private void Execute(SourceProductionContext context, ImmutableArray<PropertyInfo?> propertyInfos) {
            // Group properties by the full metadata name of their containing type
            var groupedByContainingType = propertyInfos
                .GroupBy(p => p!.ContainingType.ToDisplayString());

            foreach (var group in groupedByContainingType) {
                var classSymbol = group.First()!.ContainingType;
                var className = classSymbol.Name;
                var ns = classSymbol.ContainingNamespace?.ToDisplayString() ?? "";

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
        private static PropertyInfo? GetPropertyInfoOrNull(GeneratorSyntaxContext context) {
            // node must be PropertyDeclarationSyntax due to predicate
            var propDecl = (PropertyDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(propDecl);
            if (symbol == null) return null;

            if (symbol is not IPropertySymbol propertySymbol) { return null; }

            // Look for [IsExpressionAttribute]
            // Note to future me -- to get it working with MVVM toolkit we could scan for the [ObservableProperty] instead and generate based on that then
            var myPropAttr = symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "NINA.Sequencer.Generators.IsExpressionAttribute");

            if (myPropAttr == null) return null;

            // If found, we can also extract the ExtraInfo argument, if desired
            // (If you want to handle multiple arguments or advanced scenarios, adapt accordingly.)
            var extraInfo = (myPropAttr.ConstructorArguments.Length > 0)
                ? myPropAttr.ConstructorArguments[0].Value?.ToString() ?? ""
                : "";

            return new PropertyInfo(propertySymbol.ContainingType, propertySymbol);
        }

        private static string GeneratePartialClass(
        string namespaceName,
        string className,
        IGrouping<string, PropertyInfo?> properties
    ) {
            // Build the partial class with one method per property
            var propertiesSource = string.Empty;
            var methodsSource = string.Empty;
            
            foreach (var prop in properties) {
                if (prop is null) continue;
                string propName = prop.PropertySymbol.Name;
                string fieldName = propName.Substring(0, 1).ToLower() + propName.Substring(1);
                string fieldNameExpression = fieldName + "Expression";
                string propNameExpression = propName + "Expression";

                propertiesSource += $@"
                    private Expression {fieldNameExpression};
                    [JsonProperty]
                    public Expression {propNameExpression} {{
                        get => {fieldNameExpression};
                        set {{
                          {fieldNameExpression} = value;
                          RaisePropertyChanged();
                        }}
                    }}
                ";

                methodsSource += $@"
                    public void {propNameExpression}Setter (Expression exp) {{
                        {propNameExpression}Validation(exp);
                        if (string.IsNullOrEmpty(exp.Error)) {{
                            {propName} = exp.Value;
                        }}
                    }}
                    
                    partial void {propNameExpression}Validation (Expression exp);
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
{propertiesSource}
{methodsSource}
    }}
}}";
        }

        private sealed record PropertyInfo {
            public PropertyInfo(INamedTypeSymbol containingType, IPropertySymbol propertySymbol) {
                ContainingType = containingType;
                PropertySymbol = propertySymbol;
            }

            public INamedTypeSymbol ContainingType { get; }
            public IPropertySymbol PropertySymbol { get; }
        }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IsExpressionAttribute : Attribute {
        public IsExpressionAttribute() {
        }
    }
}
