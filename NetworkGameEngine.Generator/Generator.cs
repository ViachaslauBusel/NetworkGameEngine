using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetworkGameEngine.Generator
{
    [Generator]
    public sealed class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Берем только объявления классов + сразу тащим SemanticModel
            var classesWithSemanticModel = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is ClassDeclarationSyntax c && c.BaseList is not null,
                    transform: static (ctx, _) => ((ClassDeclarationSyntax)ctx.Node, ctx.SemanticModel))
                .Collect();

            context.RegisterSourceOutput(classesWithSemanticModel, static (spc, items) =>
            {
                List<GeneratedSource> sources = new List<GeneratedSource>();
                new CommandResultExtensionsGenerator("IReactCommandWithResultAsync<,>").GenerateSources(items, in sources);
                new CommandResultExtensionsGenerator("IReactCommandWithResult<,>").GenerateSources(items, in sources);
                new CommandResultExtensionsGenerator("IReactCommand<>").GenerateSources(items, in sources);

                foreach (var source in sources)
                {
                    spc.AddSource(source.FileName, source.SourceCode);
                }
            });
        }
    }
}