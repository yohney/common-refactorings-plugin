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
    public class AlterConstructorRewriter : CSharpSyntaxRewriter
    {
        private bool _rewriteParams;
        private FieldDeclarationSyntax _relatedField;
        private ConstructorDeclarationSyntax _targetConstructor;

        public AlterConstructorRewriter(FieldDeclarationSyntax relatedField, bool rewriteParams = true)
        {
            this._rewriteParams = rewriteParams;
            this._relatedField = relatedField;
        }

        public AlterConstructorRewriter(FieldDeclarationSyntax relatedField, ConstructorDeclarationSyntax targetConstructor, bool rewriteParams = true)
            : this(relatedField, rewriteParams)
        {
            this._targetConstructor = targetConstructor;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if(this._targetConstructor == null || node.ParameterList.ToString() == this._targetConstructor.ParameterList.ToString())
            {
                var fieldName = this._relatedField.Declaration.Variables
                    .Select(p => p.Identifier.Text)
                    .FirstOrDefault();

                var paramName = fieldName.TrimStart('_');

                if (this._rewriteParams)
                {
                    var newParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName))
                                .WithType(this._relatedField.Declaration.Type);

                    var newConstructorParams = node.ParameterList.AddParameters(newParam);

                    node = node.WithParameterList(newConstructorParams);
                }

                var newStatement = SyntaxExtenders.AssignmentStatement("this." + fieldName, paramName);
                var newStatements = node.Body.Statements.Insert(0, newStatement);

                node = node.WithBody(node.Body.WithStatements(newStatements)); 
            }

            return base.VisitConstructorDeclaration(node);
        }
    }
}
