using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using VS.CommonRefactoring.Logic.TreeProcessing;

namespace VS.CommonRefactoring.Logic
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(IntroduceFieldRefactoringProvider)), Shared]
    internal class IntroduceFieldRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            var paramSyntax = node.FirstAncestorOrSelf<ParameterSyntax>();
            if(paramSyntax != null && paramSyntax.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() != null)
            {
                var action = CodeAction.Create("Introduce field", c => IntroduceField(context.Document, paramSyntax, c));
                context.RegisterRefactoring(action);
            }
        }

        private async Task<Solution> IntroduceField(Document document, ParameterSyntax paramDecl, CancellationToken cancellationToken)
        {
            var rootNode = await document.GetSyntaxRootAsync();

            var rewriter = new IntroduceFieldRewriter(paramDecl);
            rootNode = rewriter.Visit(rootNode);

            var alterConstructorRewriter = new AlterConstructorRewriter(rewriter.GeneratedField, paramDecl.FirstAncestorOrSelf<ConstructorDeclarationSyntax>(), rewriteParams: false);
            rootNode = alterConstructorRewriter.Visit(rootNode);

            rootNode = Formatter.Format(rootNode, document.Project.Solution.Workspace);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            return document.WithSyntaxRoot(rootNode).Project.Solution;
        }
    }
}