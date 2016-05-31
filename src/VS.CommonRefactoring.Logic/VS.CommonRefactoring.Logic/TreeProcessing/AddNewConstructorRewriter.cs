using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VS.CommonRefactoring.Logic.TreeProcessing
{
    public class AddNewConstructorRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var emptyConstructor = SyntaxFactory.ConstructorDeclaration(node.Identifier.Text)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithBody(SyntaxFactory.Block());

            var insertIdx = 0;
            if (node.Members.Any(p => p is FieldDeclarationSyntax))
            {
                insertIdx = node.Members.LastIndexOf(p => p is FieldDeclarationSyntax) + 1;
            }

            node = node.WithMembers(node.Members.Insert(insertIdx, emptyConstructor));

            return base.VisitClassDeclaration(node);
        }
    }
}
