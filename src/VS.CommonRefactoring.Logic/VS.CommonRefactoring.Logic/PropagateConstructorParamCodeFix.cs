using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace VS.CommonRefactoring.Logic
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropagateConstructorParamCodeFix)), Shared]
    public class PropagateConstructorParamCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create("CS7036"); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public async sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            try
            {
                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

                var diagnostic = context.Diagnostics.First();
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var nd = root.FindToken(diagnosticSpan.Start).Parent;

                var constructorInitNode = nd.AncestorsAndSelf().OfType<ConstructorInitializerSyntax>().First();
                var classDecl = constructorInitNode.FirstAncestorOrSelf<ClassDeclarationSyntax>();

                var semanticModel = await context.Document.GetSemanticModelAsync();
                var baseConstructors = semanticModel.GetDeclaredSymbol(classDecl).BaseType.Constructors;

                if (baseConstructors.Count() != 1)
                    return;

                var baseConstructorParams = baseConstructors.First().Parameters;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Propagate constructor params",
                        createChangedDocument: c => PropagateConstructorParams(context.Document, constructorInitNode, baseConstructorParams, c),
                        equivalenceKey: "Propagate constructor params"),
                    diagnostic);
            }
            catch (Exception)
            {
                return;
            }
        }

        private async Task<Document> PropagateConstructorParams(
            Document document, 
            ConstructorInitializerSyntax constructorInitializerNode, 
            ImmutableArray<IParameterSymbol> baseConstrParams, 
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var classDecl = constructorInitializerNode.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            var constructorDecl = constructorInitializerNode.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();

            var constructorSymbol = classSymbol.Constructors
                .Where(p => p.Parameters.Count() == constructorDecl.ParameterList.Parameters.Count)
                .FirstOrDefault();

            var invArgList = constructorInitializerNode.ArgumentList;
            var declParamList = constructorDecl.ParameterList;

            int idx = -1;
            foreach(var baseP in baseConstrParams)
            {
                idx++;

                if (constructorSymbol.Parameters.Any(p => p.Type.Name == baseP.Type.Name))
                    continue;

                declParamList = declParamList.AddParameters(
                    SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier(baseP.Name))
                    .WithType(
                        SyntaxFactory.IdentifierName(baseP.Type.Name)));

                invArgList = SyntaxFactory.ArgumentList(invArgList.Arguments.Insert(idx, SyntaxFactory.Argument(SyntaxFactory.IdentifierName(baseP.Name))));
            }

            var root = await document.GetSyntaxRootAsync();

            var newConstructor = constructorDecl.WithParameterList(declParamList)
                .WithInitializer(constructorInitializerNode.WithArgumentList(invArgList));

            root = root.ReplaceNode(constructorDecl, newConstructor);


            return document.WithSyntaxRoot(root);
        }
    }
}