﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2016 SonarSource SA
 * mailto:contact@sonarsource.com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02
 */

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class FunctionNestingDepth : FunctionNestingDepthBase
    {
        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager)
                                       .DisabledByDefault();

        protected sealed override DiagnosticDescriptor Rule => rule;

        private const int DefaultValueMaximum = 3;

        [RuleParameter("max", PropertyType.Integer,
            "Maximum allowed control flow statement nesting depth.", DefaultValueMaximum)]
        public int Maximum { get; set; } = DefaultValueMaximum;

        private static readonly SyntaxKind[] FunctionKinds =
        {
            SyntaxKind.MethodDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.DestructorDeclaration,
            SyntaxKind.GetAccessorDeclaration,
            SyntaxKind.SetAccessorDeclaration,
            SyntaxKind.AddAccessorDeclaration,
            SyntaxKind.RemoveAccessorDeclaration
        };

        protected override void Initialize(ParameterLoadingAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(c => CheckFunctionNestingDepth(c), FunctionKinds);

        private void CheckFunctionNestingDepth(SyntaxNodeAnalysisContext context)
        {
            var walker = new NestingDepthWalker(Maximum, token => context.ReportDiagnostic(Diagnostic.Create(Rule, token.GetLocation(), Maximum)));
            walker.Visit(context.Node);
        }

        private class NestingDepthWalker : CSharpSyntaxWalker
        {
            private readonly NestingDepthCounter counter;

            public NestingDepthWalker(int maximumNestingDepth, Action<SyntaxToken> actionMaximumExceeded)
            {
                counter = new NestingDepthCounter(maximumNestingDepth, actionMaximumExceeded);
            }

            public override void VisitIfStatement(IfStatementSyntax node)
            {
                var isPartOfChainedElseIfClause = node.Parent != null && node.Parent.IsKind(SyntaxKind.ElseClause);
                if (isPartOfChainedElseIfClause)
                {
                    base.VisitIfStatement(node);
                }
                else
                {
                    counter.CheckNesting(node.IfKeyword, () => base.VisitIfStatement(node));
                }
            }

            public override void VisitForStatement(ForStatementSyntax node) => counter.CheckNesting(node.ForKeyword, () => base.VisitForStatement(node));

            public override void VisitForEachStatement(ForEachStatementSyntax node) => counter.CheckNesting(node.ForEachKeyword, () => base.VisitForEachStatement(node));

            public override void VisitWhileStatement(WhileStatementSyntax node) => counter.CheckNesting(node.WhileKeyword, () => base.VisitWhileStatement(node));

            public override void VisitDoStatement(DoStatementSyntax node) => counter.CheckNesting(node.DoKeyword, () => base.VisitDoStatement(node));

            public override void VisitSwitchStatement(SwitchStatementSyntax node) => counter.CheckNesting(node.SwitchKeyword, () => base.VisitSwitchStatement(node));

            public override void VisitTryStatement(TryStatementSyntax node) => counter.CheckNesting(node.TryKeyword, () => base.VisitTryStatement(node));
        }
    }
}