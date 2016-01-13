﻿/*
 * SonarLint for Visual Studio
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

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.Helpers;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace SonarLint.Rules.CSharp
{
    public abstract class CommentWordBase : DiagnosticAnalyzer
    {
        protected abstract string Word { get; }
        protected abstract DiagnosticDescriptor Rule { get; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeActionInNonGenerated(
                c =>
                {
                    var comments = c.Tree.GetCompilationUnitRoot().DescendantTrivia()
                        .Where(trivia => IsComment(trivia));

                    foreach (var comment in comments)
                    {
                        var text = comment.ToString();

                        foreach (var i in AllCaseInsensitiveIndexesOf(text, Word).Where(i => IsWordAt(text, i, Word.Length)))
                        {
                            var startLocation = comment.SpanStart + i;
                            var location = Location.Create(
                                c.Tree,
                                TextSpan.FromBounds(startLocation, startLocation + Word.Length));

                            c.ReportDiagnostic(Diagnostic.Create(Rule, location));
                        }
                    }
                });
        }

        private static bool IsComment(SyntaxTrivia trivia)
        {
            return trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
        }

        private static IEnumerable<int> AllCaseInsensitiveIndexesOf(string str, string value)
        {
            int i = 0;
            while ((i = str.IndexOf(value, i, str.Length - i, System.StringComparison.InvariantCultureIgnoreCase)) != -1)
            {
                yield return i;
                i += value.Length;
            }
        }

        private static bool IsWordAt(string str, int i, int count)
        {
            bool leftBoundary = true;
            if (i > 0)
            {
                leftBoundary = !char.IsLetterOrDigit(str[i - 1]);
            }

            bool rightBoundary = true;
            var rightOffset = i + count;
            if (rightOffset < str.Length)
            {
                rightBoundary = !char.IsLetterOrDigit(str[rightOffset]);
            }

            return leftBoundary && rightBoundary;
        }
    }
}