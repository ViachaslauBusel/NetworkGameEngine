using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace NetworkGameEngine.Generator
{
    internal class CommandResultExtensionsGenerator
    {
        private string targetInterfaceName;

        public CommandResultExtensionsGenerator(string interfaceName)
        {
            targetInterfaceName = interfaceName;
        }

        internal void GenerateSources(ImmutableArray<(ClassDeclarationSyntax Class, SemanticModel SemanticModel)> items, in List<GeneratedSource> sources)
        {
            foreach (var item in items)
            {
                var @class = item.Class;
                var semanticModel = item.SemanticModel;
                if (semanticModel.TryGetInterfaceTypeArgumentList(@class, targetInterfaceName, out var interfaces))
                {
                    foreach (var implementedInterface in interfaces)
                    {
                        ProcessInterface(semanticModel, implementedInterface, @class, in sources);
                    }
                }
            }
        }

        private void ProcessInterface(SemanticModel semanticModel, TypeArgumentListInfo implementedInterface, ClassDeclarationSyntax @class, in List<GeneratedSource> generatedSources)
        {
            if (implementedInterface.Arguments.Length is not (1 or 2))
                return;

            var commandType = implementedInterface.Arguments[0];
            ArgumentTypeInfo? resultType = implementedInterface.Arguments.Length == 2
                ? implementedInterface.Arguments[1]
                : null;

            var constructors = GetConstructorsOrDefault(semanticModel, commandType.FullName);

            var sourceBuilder = new StringBuilder(1024);
            AppendHeader(sourceBuilder, CollectUsings(commandType, resultType, constructors));
            AppendNamespaceAndClassStart(sourceBuilder, commandType.Namespace, commandType.TypeName, resultType is null ? "VOID" : resultType.Value.TypeName);

            foreach (var ctor in constructors)
            {
                if (resultType is null)
                    AppendVoidExtensionMethod(sourceBuilder, commandType.TypeName, ctor);
                else
                    AppendResultExtensionMethod(sourceBuilder, commandType.TypeName, resultType.Value.TypeName, ctor);
            }

            AppendClassAndNamespaceEnd(sourceBuilder);

            generatedSources.Add(new GeneratedSource(BuildFileName(commandType, resultType), sourceBuilder.ToString()));
        }

        private static TypeArgumentListInfo[] GetConstructorsOrDefault(SemanticModel semanticModel, string fullTypeName)
        {
            semanticModel.TryGetConstructors(fullTypeName, out var constructors);

            return constructors.Length == 0
                ? [new TypeArgumentListInfo(System.Array.Empty<ArgumentTypeInfo>())]
                : constructors;
        }

        private static string BuildFileName(ArgumentTypeInfo commandType, ArgumentTypeInfo? resultType)
        {
            return resultType is null ? $"{commandType.TypeName}.g.cs"
                                      : $"{commandType.TypeName}_{resultType.Value.TypeName}.g.cs";
        }

        private static List<string> CollectUsings(
            ArgumentTypeInfo commandType,
            ArgumentTypeInfo? resultType,
            TypeArgumentListInfo[] constructors)
        {
            var namespaces = new HashSet<string>
            {
                "NetworkGameEngine",
                "NetworkGameEngine.JobsSystem"
            };

            AddIfNotEmpty(namespaces, commandType.Namespace);
            if (resultType is not null)
                AddIfNotEmpty(namespaces, resultType.Value.Namespace);

            foreach (var ctor in constructors)
            {
                foreach (var parameterType in ctor.Arguments)
                    AddIfNotEmpty(namespaces, parameterType.Namespace);
            }

            return namespaces
                .OrderBy(static x => x)
                .ToList();
        }

        private static void AddIfNotEmpty(HashSet<string> set, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                set.Add(value);
        }

        private static void AppendHeader(StringBuilder sb, List<string> usingDirectives)
        {
            foreach (var u in usingDirectives)
                sb.AppendLine($"using {u};");

            sb.AppendLine();
        }

        private static void AppendNamespaceAndClassStart(StringBuilder sb, string @namespace, string className, string ext)
        {
            sb.AppendLine($"namespace {@namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {className}_{ext}");
            sb.AppendLine("    {");
        }

        private static void AppendResultExtensionMethod(StringBuilder sb, string commandTypeName, string resultTypeName, TypeArgumentListInfo ctor)
        {
            sb.Append($"        public static Job<CommandResult<{resultTypeName}>> {commandTypeName.Replace("Command", "")}Async(this GameObject gameObject");

            for (int i = 0; i < ctor.Arguments.Length; i++)
            {
                var p = ctor.Arguments[i];
                sb.Append($", {p.TypeName} {p.ArgName}");
            }

            sb.AppendLine(")");
            sb.AppendLine("        {");
            sb.Append($"            var command = new {commandTypeName}(");

            for (int i = 0; i < ctor.Arguments.Length; i++)
            {
                var p = ctor.Arguments[i];
                if (i > 0) sb.Append(", ");
                sb.Append($"{p.ArgName}");
            }

            sb.AppendLine(");");
            sb.AppendLine($"            return gameObject.SendCommandAndReturnResult<{commandTypeName}, {resultTypeName}>(command);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void AppendVoidExtensionMethod(StringBuilder sb, string commandTypeName, TypeArgumentListInfo ctor)
        {
            sb.Append($"        public static void {commandTypeName.Replace("Command", "")}(this GameObject gameObject");

            for (int i = 0; i < ctor.Arguments.Length; i++)
            {
                var p = ctor.Arguments[i];
                sb.Append($", {p.TypeName} arg_{i}");
            }

            sb.AppendLine(")");
            sb.AppendLine("        {");
            sb.Append($"            var command = new {commandTypeName}(");

            for (int i = 0; i < ctor.Arguments.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"arg_{i}");
            }

            sb.AppendLine(");");
            sb.AppendLine($"            gameObject.SendCommand<{commandTypeName}>(command);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void AppendClassAndNamespaceEnd(StringBuilder sb)
        {
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
    }
}