using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VS.CommonRefactoring.Logic.Util;

namespace VS.CommonRefactoring.Logic.TreeProcessing
{
    public class IntroduceFieldRewriter : CSharpSyntaxRewriter
    {
        private ParameterSyntax _relatedParam;

        public FieldDeclarationSyntax GeneratedField { get; private set; }

        public IntroduceFieldRewriter(ParameterSyntax relatedParam)
        {
            this._relatedParam = relatedParam;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var newName = "_" + this._relatedParam.Identifier.Text;
            var newField = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        this._relatedParam.Type, 
                        SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                            SyntaxFactory.VariableDeclarator(newName))))
                .WithModifiers(SyntaxFactory.TokenList().Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));

            this.GeneratedField = newField;

            var insertIdx = 0;
            if (node.Members.Any(p => p is FieldDeclarationSyntax))
            {
                insertIdx = node.Members.LastIndexOf(p => p is FieldDeclarationSyntax) + 1;
            }

            node = node.WithMembers(node.Members.Insert(insertIdx, newField));

            return base.VisitClassDeclaration(node);
        }
    }
}
