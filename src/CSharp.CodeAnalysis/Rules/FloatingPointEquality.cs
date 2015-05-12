﻿/*
 * SonarQube C# Code Analysis
 * Copyright (C) 2015 SonarSource
 * dev@sonar.codehaus.org
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
 
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarQube.CSharp.CodeAnalysis.Helpers;
using SonarQube.CSharp.CodeAnalysis.SonarQube.Settings;
using SonarQube.CSharp.CodeAnalysis.SonarQube.Settings.Sqale;

namespace SonarQube.CSharp.CodeAnalysis.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SqaleConstantRemediation("5min")]
    [SqaleSubCharacteristic(SqaleSubCharacteristic.InstructionReliability)]
    [Rule(DiagnosticId, RuleSeverity, Description, IsActivatedByDefault)]
    [Tags("bug", "misra")]
    public class FloatingPointEquality : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1244";
        internal const string Description = "Floating point numbers should not be tested for equality";
        internal const string MessageFormat = "Use \"<=\" or \">=\" instead of \"==\" and \"!=\".";
        internal const string Category = "SonarQube";
        internal const Severity RuleSeverity = Severity.Critical;
        internal const bool IsActivatedByDefault = true;

        internal static DiagnosticDescriptor Rule = 
            new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, 
                RuleSeverity.ToDiagnosticSeverity(), IsActivatedByDefault, 
                helpLinkUri: "http://nemo.sonarqube.org/coding_rules#rule_key=csharpsquid%3AS1244");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private static readonly SpecialType[] FloatingPointTypes = 
        {
            SpecialType.System_Single,
            SpecialType.System_Double
        };

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                c =>
                {
                    var equals = (BinaryExpressionSyntax) c.Node;
                    var equalitySymbol = c.SemanticModel.GetSymbolInfo(equals).Symbol as IMethodSymbol;

                    if (equalitySymbol != null &&
                        equalitySymbol.Name == "op_Equality" &&
                        equalitySymbol.ContainingType != null &&
                        FloatingPointTypes.Contains(equalitySymbol.ContainingType.SpecialType))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Rule, equals.OperatorToken.GetLocation()));
                    }
                },
                SyntaxKind.EqualsExpression
                );
            context.RegisterSyntaxNodeAction(
                c =>
                {
                    var binaryExpression = (BinaryExpressionSyntax) c.Node;
                    var left = TryGetBinaryExpression(binaryExpression.Left);
                    var right = TryGetBinaryExpression(binaryExpression.Right);

                    if (right == null || left == null)
                    {
                        return;
                    }

                    var eqRight = EquivalenceChecker.AreEquivalent(right.Right, left.Right);
                    var eqLeft = EquivalenceChecker.AreEquivalent(right.Left, left.Left);
                    if (!eqRight || !eqLeft)
                    {
                        return;
                    }

                    if (IsIndirectEquality(binaryExpression, right, left, c) ||
                        IsIndirectInequality(binaryExpression, right, left, c))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Rule, binaryExpression.GetLocation()));
                    }
                },
                SyntaxKind.LogicalAndExpression,
                SyntaxKind.LogicalOrExpression
                );
        }

        private static BinaryExpressionSyntax TryGetBinaryExpression(ExpressionSyntax expression)
        {
            var currentExpression = expression;
            while (currentExpression is ParenthesizedExpressionSyntax)
            {
                currentExpression = ((ParenthesizedExpressionSyntax) currentExpression).Expression;
            }

            return currentExpression as BinaryExpressionSyntax;
        }

        private static bool IsIndirectInequality(BinaryExpressionSyntax binaryExpression, BinaryExpressionSyntax right, BinaryExpressionSyntax left, SyntaxNodeAnalysisContext c)
        {
            return binaryExpression.IsKind(SyntaxKind.LogicalOrExpression) &&
                   HasAppropriateOperatorsForInequality(right, left) &&
                   HasFloatingType(right.Right, right.Left, c.SemanticModel);
        }

        private static bool IsIndirectEquality(BinaryExpressionSyntax binaryExpression, BinaryExpressionSyntax right, BinaryExpressionSyntax left, SyntaxNodeAnalysisContext c)
        {
            return binaryExpression.IsKind(SyntaxKind.LogicalAndExpression) &&
                   HasAppropriateOperatorsForEquality(right, left) &&
                   HasFloatingType(right.Right, right.Left, c.SemanticModel);
        }

        private static bool HasFloatingType(ExpressionSyntax right, ExpressionSyntax left, SemanticModel semanticModel)
        {
            var rightType = semanticModel.GetTypeInfo(right);
            if (rightType.Type != null && FloatingPointTypes.Contains(rightType.Type.SpecialType))
            {
                return true;
            }

            var leftType = semanticModel.GetTypeInfo(left);
            if (leftType.Type != null && FloatingPointTypes.Contains(leftType.Type.SpecialType))
            {
                return true;
            }

            return false;
        }

        private static bool HasAppropriateOperatorsForEquality(BinaryExpressionSyntax right, BinaryExpressionSyntax left)
        {
            return new[] {right.OperatorToken.Kind(), left.OperatorToken.Kind()}
                .Intersect(new[] {SyntaxKind.LessThanEqualsToken, SyntaxKind.GreaterThanEqualsToken})
                .Count() == 2;
        }
        private static bool HasAppropriateOperatorsForInequality(BinaryExpressionSyntax right, BinaryExpressionSyntax left)
        {
            return new[] { right.OperatorToken.Kind(), left.OperatorToken.Kind() }
                .Intersect(new[] { SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken })
                .Count() == 2;
        }
    }
}