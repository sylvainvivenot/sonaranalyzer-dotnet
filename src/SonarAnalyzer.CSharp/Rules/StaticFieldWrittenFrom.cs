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

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SonarAnalyzer.Helpers;
using System.Linq;

namespace SonarAnalyzer.Rules
{
    public abstract class StaticFieldWrittenFrom : SonarDiagnosticAnalyzer
    {
        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterCodeBlockStartActionInNonGenerated<SyntaxKind>(
               cbc =>
               {
                   if (!IsValidCodeBlockContext(cbc.CodeBlock, cbc.OwningSymbol))
                   {
                       return;
                   }

                   var locationsForFields = new Dictionary<IFieldSymbol, List<Location>>();

                   cbc.RegisterSyntaxNodeAction(
                       c =>
                       {
                           var assignment = (AssignmentExpressionSyntax)c.Node;
                           var expression = assignment.Left;

                           var fieldSymbol = c.SemanticModel.GetSymbolInfo(expression).Symbol as IFieldSymbol;
                           if (IsStatic(fieldSymbol))
                           {
                               var location = Location.Create(expression.SyntaxTree,
                                   new TextSpan(expression.SpanStart,
                                       assignment.OperatorToken.Span.End - expression.SpanStart));

                               AddFieldLocation(fieldSymbol, location, locationsForFields);
                           }
                       },
                       SyntaxKind.SimpleAssignmentExpression,
                       SyntaxKind.AddAssignmentExpression,
                       SyntaxKind.SubtractAssignmentExpression,
                       SyntaxKind.MultiplyAssignmentExpression,
                       SyntaxKind.DivideAssignmentExpression,
                       SyntaxKind.ModuloAssignmentExpression,
                       SyntaxKind.AndAssignmentExpression,
                       SyntaxKind.ExclusiveOrAssignmentExpression,
                       SyntaxKind.OrAssignmentExpression,
                       SyntaxKind.LeftShiftAssignmentExpression,
                       SyntaxKind.RightShiftAssignmentExpression);

                   cbc.RegisterSyntaxNodeAction(
                       c =>
                       {
                           var unary = (PrefixUnaryExpressionSyntax)c.Node;
                           CollectLocationOfStaticField(unary.Operand, locationsForFields, c);
                       },
                       SyntaxKind.PreDecrementExpression,
                       SyntaxKind.PreIncrementExpression);

                   cbc.RegisterSyntaxNodeAction(
                       c =>
                       {
                           var unary = (PostfixUnaryExpressionSyntax)c.Node;
                           CollectLocationOfStaticField(unary.Operand, locationsForFields, c);
                       },
                       SyntaxKind.PostDecrementExpression,
                       SyntaxKind.PostIncrementExpression);

                   cbc.RegisterCodeBlockEndAction(c =>
                   {
                       foreach (var fieldWithLocations in locationsForFields)
                       {
                           var firstPosition = fieldWithLocations.Value.Select(loc => loc.SourceSpan.Start).Min();
                           var location = fieldWithLocations.Value.First(loc => loc.SourceSpan.Start == firstPosition);
                           var message = GetDiagnosticMessageArgument(cbc.CodeBlock, cbc.OwningSymbol, fieldWithLocations.Key);
                           c.ReportDiagnostic(Diagnostic.Create(Rule, location, message));
                       }
                   });
               });
        }

        protected abstract bool IsValidCodeBlockContext(SyntaxNode node, ISymbol owningSymbol);

        protected abstract string GetDiagnosticMessageArgument(SyntaxNode node, ISymbol owningSymbol, IFieldSymbol field);

        private static void AddFieldLocation(IFieldSymbol fieldSymbol, Location location, Dictionary<IFieldSymbol, List<Location>> locationsForFields)
        {
            if (!locationsForFields.ContainsKey(fieldSymbol))
            {
                locationsForFields.Add(fieldSymbol, new List<Location>());
            }

            locationsForFields[fieldSymbol].Add(location);
        }

        private static void CollectLocationOfStaticField(ExpressionSyntax expression, Dictionary<IFieldSymbol, List<Location>> locationsForFields, SyntaxNodeAnalysisContext context)
        {
            var fieldSymbol = context.SemanticModel.GetSymbolInfo(expression).Symbol as IFieldSymbol;
            if (IsStatic(fieldSymbol))
            {
                AddFieldLocation(fieldSymbol, expression.GetLocation(), locationsForFields);
            }
        }

        private static bool IsStatic(IFieldSymbol fieldSymbol)
        {
            return fieldSymbol != null &&
                fieldSymbol.IsStatic;
        }
    }
}
