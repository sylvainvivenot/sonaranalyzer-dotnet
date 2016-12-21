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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using System.Linq;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class StaticFieldWrittenFromInstanceConstructor : StaticFieldWrittenFrom
    {
        internal const string DiagnosticId = "S3010";
        internal const string MessageFormat = "Remove this assignment of '{0}' or initialize it statically.";

        protected static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        protected override DiagnosticDescriptor Rule => rule;

        protected override bool IsValidCodeBlockContext(SyntaxNode node, ISymbol owningSymbol)
        {
            var declaration = node as ConstructorDeclarationSyntax;
            if (declaration == null)
            {
                return false;
            }

            if (declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                return false;
            }

            return true;
        }

        protected override string GetDiagnosticMessageArgument(SyntaxNode node, ISymbol owningSymbol, IFieldSymbol field)
        {
            return field.Name;
        }
    }
}
