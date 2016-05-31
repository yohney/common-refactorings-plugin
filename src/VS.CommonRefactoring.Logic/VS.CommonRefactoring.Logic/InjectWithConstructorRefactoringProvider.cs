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
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(InjectWithConstructorRefactoringProvider)), Shared]
    internal class InjectWithConstructorRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            var fieldDecl = node.FirstAncestorOrSelf<FieldDeclarationSyntax>();
            if (fieldDecl != null)
            {
                var action = CodeAction.Create("Inject via constructor", c => InjectViaConstructor(context.Document, fieldDecl, c));
                context.RegisterRefactoring(action);
            }
        }

        private async Task<Solution> InjectViaConstructor(Document document, FieldDeclarationSyntax fieldDecl, CancellationToken cancellationToken)
        {
            var rootNode = await document.GetSyntaxRootAsync();

            var constrLocatorVisitor = new ConstructorLocatorVisitor();
            var targetConstructor = constrLocatorVisitor.GetTargetConstructor(rootNode);

            if(targetConstructor == null)
            {
                // Need to add new constructor
                var newConstructorWriter = new AddNewConstructorRewriter();
                rootNode = newConstructorWriter.Visit(rootNode);
            }

            var rewriter = new AlterConstructorRewriter(fieldDecl, targetConstructor);
            rootNode = rewriter.Visit(rootNode);

            rootNode = Formatter.Format(rootNode, document.Project.Solution.Workspace);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            return document.WithSyntaxRoot(rootNode).Project.Solution;
        }
    }
}