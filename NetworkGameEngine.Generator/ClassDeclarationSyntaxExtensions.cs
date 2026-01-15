using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetworkGameEngine.Generator
{
    public readonly struct ArgumentTypeInfo
    {
        public string TypeName { get; }
        public string ArgName { get; }
        public string Namespace { get; }
        public string FullName { get; }

        public ArgumentTypeInfo(string typeName, string argName, string @namespace, string fullName)
        {
            TypeName = typeName;
            ArgName = argName;
            Namespace = @namespace;
            FullName = fullName;
        }

        public ArgumentTypeInfo(string typeName, string argName, string @namespace)
        {
            TypeName = typeName;
            ArgName = argName;
            Namespace = @namespace;
            FullName = string.IsNullOrEmpty(@namespace) ? typeName : $"{@namespace}.{typeName}";
        }
    }

    //Используется для хранения информации о типах интерфейса и о типах аргументов конструктора
    public readonly struct TypeArgumentListInfo
    {
        private readonly ArgumentTypeInfo[] _arguments;
        public ArgumentTypeInfo[] Arguments => _arguments;

        public TypeArgumentListInfo(ArgumentTypeInfo[] arguments)
        {
            _arguments = arguments;
        }
    }

    internal static class ClassDeclarationSyntaxExtensions
    {
        public static bool TryGetInterfaceTypeArgumentList(this SemanticModel semanticModel, ClassDeclarationSyntax @class,
                                                           string interfaceMetadataName, out List<TypeArgumentListInfo> foundInterfaces)
        {
            foundInterfaces = null;

            var shortNameNoArity = interfaceMetadataName.Split('<')[0]; // can be "Namespace.IName" or "IName"
            var interfaceName = shortNameNoArity.Split('.').Last();
            var expectedArity = GetArityFromMetadataName(interfaceMetadataName);

            foreach (var baseType in @class.BaseList?.Types ?? default)
            {
                if (baseType.Type is not GenericNameSyntax generic)
                    continue;

                if (!string.Equals(generic.Identifier.ValueText, interfaceName, StringComparison.Ordinal))
                    continue;

                if (generic.Arity != expectedArity)
                    continue;

                foundInterfaces ??= new List<TypeArgumentListInfo>();

                var args = generic.TypeArgumentList.Arguments
                    .Select(a => ToArgumentTypeInfo(semanticModel, a))
                    .ToArray();

                foundInterfaces.Add(new TypeArgumentListInfo(args));
            }

            return foundInterfaces is not null;
        }

        private static ArgumentTypeInfo ToArgumentTypeInfo(SemanticModel semanticModel, TypeSyntax typeSyntax)
        {
            // ВАЖНО: namespace берется с ITypeSymbol (semantic), не из syntax.
            var typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;

            if (typeSymbol is null)
                return new ArgumentTypeInfo(typeSyntax.ToString(), string.Empty, string.Empty, typeSyntax.ToString());

            var ns = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

            var fullName = typeSymbol
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "");
            return new ArgumentTypeInfo(typeSymbol.Name, string.Empty, ns, fullName);
        }

        private static int GetArityFromMetadataName(string metadataName)
        {
            var lt = metadataName.IndexOf('<');
            if (lt < 0) return 0;

            var inside = metadataName.Substring(lt + 1, metadataName.Length - lt - 2);
            return inside.Length == 0 ? 1 : inside.Count(c => c == ',') + 1;
        }

        public static bool TryGetConstructors(
            this SemanticModel semanticModel,
            string typeFullName,
            out TypeArgumentListInfo[] constructors)
        {
            constructors = Array.Empty<TypeArgumentListInfo>();

            // GetTypeByMetadataName ожидает: "Namespace.Type" (+ вложенные как Outer+Inner)
            var namedType = semanticModel.Compilation.GetTypeByMetadataName(typeFullName) as INamedTypeSymbol;
            if (namedType is null)
                return false;

            constructors = namedType.Constructors
                .Where(c => !c.IsImplicitlyDeclared) // обычно отбрасывают синтетический .ctor()
                .Select(c => new TypeArgumentListInfo(
                    c.Parameters
                     .Select(static p =>
                     {
                         var typeSymbol = p.Type;

                         // "int?" / "string?" / "MyType?" etc.
                         // If nullable annotations are enabled in the consuming project, this preserves them.
                         var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                         var argName = p.Name;

                         var ns = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

                         return new ArgumentTypeInfo(typeName, argName, ns);
                     })
                        .ToArray()
                ))
                .ToArray();

            return true;
        }
    }
}