using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RecordInterfaces.SourceGenerators;

using NodeDescriptor = (
    RecordDeclarationSyntax Node,
    bool ImplementsRecordInterfaceAttributeFound);

using PropertyDescriptor = (
    string Type,
    string Name);

// [Generator]
public sealed class RecordIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
           .SyntaxProvider
           .CreateSyntaxProvider(
                (node, _) => node is RecordDeclarationSyntax,
                (ctx, _) => GetRecordDeclarationForSourceGen(ctx))
           .Where(x => x.ImplementsRecordInterfaceAttributeFound)
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
        var recordDeclarationSyntax = (RecordDeclarationSyntax) context.Node;

        var attributesSyntax = recordDeclarationSyntax
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
            if (attributeName == "RecordInterfaces.Abstractions.ImplementsRecordInterface")
                return (recordDeclarationSyntax, true);
        }

        return (recordDeclarationSyntax, false);
    }

    private static void GenerateCode(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<RecordDeclarationSyntax> recordDeclarations)
    {
        foreach (var recordDeclaration in recordDeclarations)
        {
            var baseList = recordDeclaration.BaseList;

            if (baseList is null)
                continue;

            var root = recordDeclaration.SyntaxTree.GetRoot();
            var rootSemanticModel = compilation.GetSemanticModel(root.SyntaxTree);

            var recordSemanticModel = compilation.GetSemanticModel(recordDeclaration.SyntaxTree);

            if (recordSemanticModel.GetDeclaredSymbol(recordDeclaration) is not INamedTypeSymbol recordSymbol)
                continue;

            foreach (var interfaceDeclaration in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                if (rootSemanticModel.GetDeclaredSymbol(interfaceDeclaration) is not INamedTypeSymbol interfaceSymbol)
                    continue;

                var isRecordInterface = interfaceSymbol
                   .GetAttributes()
                   .Any(x => x.AttributeClass!.ToDisplayString() ==
                             "RecordInterfaces.Abstractions.RecordInterfaceAttribute");

                if (!isRecordInterface)
                    continue;

                var propertyDeclarations = interfaceDeclaration
                   .DescendantNodes()
                   .OfType<PropertyDeclarationSyntax>()
                   .ToImmutableArray();

                var interfaceSemanticModel = compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);
                List<PropertyDescriptor> propertyDescriptors = [];

                foreach (var propertyDeclaration in propertyDeclarations)
                {
                    if (interfaceSemanticModel.GetDeclaredSymbol(propertyDeclaration) is not IPropertySymbol property)
                        continue;

                    propertyDescriptors.Add((property.Type.Name, property.Name));
                }

                var @namespace = recordSymbol.ContainingNamespace.ToDisplayString();
                var @interface = interfaceDeclaration.Identifier.Text;
                var record = recordDeclaration.Identifier.Text;

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
                    sb.Append(",");
                }

                var @params = sb.ToString(0, sb.Length - 1);

                sb.Clear();

                foreach ((string type, string name) in propertyDescriptors)
                {
                    sb.Append("        ");
                    sb.Append($"[UnsafeAccessor(UnsafeAccessorKind.Method, Name = \"set_{name}\")]\n");
                    sb.Append("        ");
                    sb.Append($"static extern void Set{name}({record} @this, {type} value);\n\n");
                }

                var accessors = sb.ToString(0, sb.Length - 2);

                sb.Clear();

                foreach ((string _, string name) in propertyDescriptors)
                {
                    var parameterName = char.ToLower(name[0]) + name[1..];

                    var setter =
                        $"""
                                 if ({parameterName}.HasValue)
                                     Set{name}(clone, {parameterName}.Value);

                        """;

                    sb.Append(setter);
                }

                var setters = sb.ToString(0, sb.Length - 1);

                var code =
                    $$"""
                      // <auto-generated/>

                      using Microsoft.CodeAnalysis;
                      using System.Runtime.CompilerServices;

                      namespace {{@namespace}};

                      partial record {{record}}
                      {
                          {{@interface}} {{@interface}}.With({{@params}})
                          {
                              [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "<Clone>$")]
                              static extern {{record}} Clone({{record}} @this);
                              
                      {{accessors}}
                              
                              var clone = Clone(this);

                      {{setters}}
                      
                              return clone;
                          }
                      }

                      """;

                context.AddSource($"{record}.{@interface}.g.cs", SourceText.From(code, Encoding.UTF8));
            }
        }
    }
}
