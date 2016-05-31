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
    public class ConstructorLocatorVisitor : CSharpSyntaxWalker
    {
        private bool _didVisit;
        private List<ConstructorDeclarationSyntax> _nonStaticConstructors;

        public ConstructorLocatorVisitor()
        {
            this._nonStaticConstructors = new List<ConstructorDeclarationSyntax>();
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if(!node.Modifiers.Any(predicate => predicate.Kind() == SyntaxKind.StaticKeyword))
            {
                this._nonStaticConstructors.Add(node);
            }

            base.VisitConstructorDeclaration(node);
        }

        public ConstructorDeclarationSyntax GetTargetConstructor(SyntaxNode rootNode)
        {
            if(!this._didVisit)
            {
                this.Visit(rootNode);
                this._didVisit = true;
            }

            if (this._nonStaticConstructors.Count == 0)
                return null;

            if (this._nonStaticConstructors.Count == 1)
                return this._nonStaticConstructors.First();

            var nonEmpty = this._nonStaticConstructors.Where(p => p.ParameterList.Parameters.Count > 0)
                .ToList();

            return nonEmpty.FirstOrDefault();
        }
    }
}
