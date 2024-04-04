using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RecordInterfaces.Abstractions;

namespace RecordInterfaces.SourceGenerators;

using NodeDescriptor = (InterfaceDeclarationSyntax Node, bool RecordInterfaceAttributeFound);

[Generator]
public sealed class InterfaceIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
           .SyntaxProvider
           .CreateSyntaxProvider(
                (node, _) => node is InterfaceDeclarationSyntax,
                (ctx, _) => GetRecordDeclarationForSourceGen(ctx))
           .Where(x => x.RecordInterfaceAttributeFound)
           .Select((x, _) => x.Node);

        var source = context
           .CompilationProvider
           .Combine(provider.Collect());

        context.RegisterSourceOutput(
            source,
            (ctx, src) => GenerateCode(ctx, src.Left, src.Right));
    }

    private static NodeDescriptor GetRecordDeclarationForSourceGen(GeneratorSyntaxContext context)
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

            var recordInterfaceAttributeType = typeof(RecordInterfaceAttribute);

            // TODO: extension method
            if (attributeName == $"{recordInterfaceAttributeType.Namespace}.{recordInterfaceAttributeType.Name}")
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

            foreach (var propertyDeclaration in propertyDeclarations)
            {
                var property = semanticModel.GetDeclaredSymbol(propertyDeclaration);
            }
        }
    }
}
