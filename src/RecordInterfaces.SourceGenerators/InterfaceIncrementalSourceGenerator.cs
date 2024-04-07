using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RecordInterfaces.SourceGenerators;

using NodeDescriptor = (
    InterfaceDeclarationSyntax Node,
    bool RecordInterfaceAttributeFound);

using PropertyDescriptor = (
    string Type,
    string Name);

[Generator]
public sealed class InterfaceIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
           .SyntaxProvider
           .CreateSyntaxProvider(
                (node, _) => node is InterfaceDeclarationSyntax,
                (ctx, _) => GetInterfaceDeclarationForSourceGen(ctx))
           .Where(x => x.RecordInterfaceAttributeFound)
           .Select((x, _) => x.Node);

        var source = context
           .CompilationProvider
           .Combine(provider.Collect());

        context.RegisterSourceOutput(
            source,
            (ctx, src) => GenerateCode(ctx, src.Left, src.Right));
    }

    private static NodeDescriptor GetInterfaceDeclarationForSourceGen(GeneratorSyntaxContext context)
    {
        var interfaceDeclarationSyntax = (InterfaceDeclarationSyntax) context.Node;

        var attributesSyntax = interfaceDeclarationSyntax
           .AttributeLists
           .SelectMany(x => x.Attributes);

        foreach (var attributeSyntax in attributesSyntax)
        {
            var maybeAttributeSymbol = context
               .SemanticModel
               .GetSymbolInfo(attributeSyntax)
               .Symbol;

            if (maybeAttributeSymbol is not IMethodSymbol attributeSymbol)
                continue;

            var attributeName = attributeSymbol
               .ContainingType
               .ToDisplayString();

            // TODO: fix, typeof() does not work
            if (attributeName == "RecordInterfaces.Abstractions.RecordInterfaceAttribute")
                return (interfaceDeclarationSyntax, true);
        }

        return (interfaceDeclarationSyntax, false);
    }

    private static void GenerateCode(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<InterfaceDeclarationSyntax> interfaceDeclarations)
    {
        foreach (var interfaceDeclaration in interfaceDeclarations)
        {
            var propertyDeclarations = interfaceDeclaration
               .DescendantNodes()
               .OfType<PropertyDeclarationSyntax>()
               .ToImmutableArray();

            var semanticModel = compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(interfaceDeclaration) is not INamedTypeSymbol interfaceSymbol)
                continue;

            List<PropertyDescriptor> propertyDescriptors = [];

            foreach (var propertyDeclaration in propertyDeclarations)
            {
                if (semanticModel.GetDeclaredSymbol(propertyDeclaration) is not IPropertySymbol property)
                    continue;

                propertyDescriptors.Add((property.Type.Name, property.Name));
            }

            var @namespace = interfaceSymbol.ContainingNamespace.ToDisplayString();
            var @interface = interfaceDeclaration.Identifier.Text;

            StringBuilder sb = new();

            foreach ((string type, string name) in propertyDescriptors)
            {
                sb.Append("\n        ");
                sb.Append("Optional");
                sb.Append("<");
                sb.Append(type);
                sb.Append('>');
                sb.Append(' ');
                sb.Append(char.ToLower(name[0]) + name[1..]);
                sb.Append(" = default,");
            }

            string @params = sb.ToString(0, sb.Length - 1);

            var code =
                $$"""
                  // <auto-generated/>

                  using Microsoft.CodeAnalysis;

                  namespace {{@namespace}};

                  partial interface {{@interface}}
                  {
                      {{@interface}} With({{@params}}) 
                      {
                          throw new NotImplementedException(
                            $"`{nameof(With)}` method is not implemented on type {GetType().Name}");
                      }
                  }

                  """;

            context.AddSource($"{@interface}.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}
