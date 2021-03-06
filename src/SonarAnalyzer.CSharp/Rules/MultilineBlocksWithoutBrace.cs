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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class MultilineBlocksWithoutBrace : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2681";
        internal const string MessageFormat =
            "This line will not be executed {0}; only the first line of this {2}-line block will be. The rest will execute {1}.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        protected sealed override DiagnosticDescriptor Rule => rule;

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckLoop(c, ((WhileStatementSyntax) c.Node).Statement),
                SyntaxKind.WhileStatement);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckLoop(c, ((ForStatementSyntax) c.Node).Statement),
                SyntaxKind.ForStatement);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckLoop(c, ((ForEachStatementSyntax) c.Node).Statement),
                SyntaxKind.ForEachStatement);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckIf(c, (IfStatementSyntax) c.Node),
                SyntaxKind.IfStatement);
        }

        private static void CheckLoop(SyntaxNodeAnalysisContext context, StatementSyntax statement)
        {
            if (IsNestedStatement(statement))
            {
                return;
            }

            CheckStatement(context, statement, "in a loop", "only once");
        }

        private static void CheckIf(SyntaxNodeAnalysisContext context, IfStatementSyntax ifStatement)
        {
            if (ifStatement.GetPrecedingIfsInConditionChain().Any())
            {
                return;
            }

            if (IsNestedStatement(ifStatement.Statement))
            {
                return;
            }

            var lastStatementInIfChain = GetLastStatementInIfChain(ifStatement);
            if (IsStatementCandidateLoop(lastStatementInIfChain))
            {
                return;
            }

            CheckStatement(context, lastStatementInIfChain, "conditionally", "unconditionally");
        }

        private static bool IsNestedStatement(StatementSyntax nested)
        {
            return nested is IfStatementSyntax ||
                nested is ForStatementSyntax ||
                nested is ForEachStatementSyntax ||
                nested is WhileStatementSyntax;
        }

        private static StatementSyntax GetLastStatementInIfChain(IfStatementSyntax ifStatement)
        {
            var currentIfStatement = ifStatement;
            var statement = currentIfStatement.Statement;
            while (currentIfStatement != null)
            {
                if (currentIfStatement.Else == null)
                {
                    return currentIfStatement.Statement;
                }

                statement = currentIfStatement.Else.Statement;
                currentIfStatement = statement as IfStatementSyntax;
            }

            return statement;
        }

        private static void CheckStatement(SyntaxNodeAnalysisContext context, StatementSyntax statement,
            string executed, string execute)
        {
            if (statement is BlockSyntax)
            {
                return;
            }

            var nextStatement = context.Node.GetLastToken().GetNextToken().Parent;
            if (nextStatement == null)
            {
                return;
            }

            var statementPosition = statement.GetLocation().GetLineSpan().StartLinePosition;
            var nextStatementPosition = nextStatement.GetLocation().GetLineSpan().StartLinePosition;

            if (statementPosition.Character == nextStatementPosition.Character)
            {
                var lineSpan = context.Node.SyntaxTree.GetText().Lines[nextStatementPosition.Line].Span;
                var location = Location.Create(context.Node.SyntaxTree, TextSpan.FromBounds(nextStatement.SpanStart, lineSpan.End));

                context.ReportDiagnostic(Diagnostic.Create(rule, location, executed, execute,
                    nextStatementPosition.Line - statementPosition.Line + 1));
            }
        }

        private static bool IsStatementCandidateLoop(StatementSyntax statement)
        {
            return statement is ForEachStatementSyntax ||
                statement is ForStatementSyntax ||
                statement is WhileStatementSyntax;
        }
    }
}
