using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NetworkGameEngine.Generator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {


        }

        public void Initialize(GeneratorInitializationContext context)
        {

            var gameObjectText = SourceText.From(@"

", Encoding.UTF8);



            // Register the attribute source
            context.RegisterForPostInitialization((i) =>
            {
                i.AddSource("GameObject.g.cs", gameObjectText);
            });

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

    }
    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    public class SyntaxReceiver : ISyntaxContextReceiver
    {
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax component)
            {

            }
        }
    }
}