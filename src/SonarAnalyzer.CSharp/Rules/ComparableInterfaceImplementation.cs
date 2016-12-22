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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    public class ComparableInterfaceImplementation : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1210";
        internal const string MessageFormat = "When implementing {0}, you should also override {1}.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        protected sealed override DiagnosticDescriptor Rule => rule;

        private const string ObjectEquals = nameof(object.Equals);

        private static readonly ISet<KnownType> ComparableInterfaces = ImmutableHashSet.Create(
            KnownType.System_IComparable,
            KnownType.System_IComparable_T);

        private static readonly IList<string> RequiredOperators = ImmutableList.Create(
            "op_LessThan",
            "op_GreaterThan",
            "op_Equality",
            "op_Inequality");

        private static readonly IDictionary<string, string> OperatorNamesMap = new Dictionary<string, string>
        {
            { "op_LessThan", "<" },
            { "op_GreaterThan", ">" },
            { "op_Equality", "==" },
            { "op_Inequality" , "!=" },
        }.ToImmutableDictionary();

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var classDeclaration = (ClassDeclarationSyntax)c.Node;

                    var classSymbol = c.SemanticModel.GetDeclaredSymbol(classDeclaration);
                    if (classSymbol == null)
                    {
                        return;
                    }

                    var baseImplementsIComparable = classSymbol
                        .BaseType
                        .GetSelfAndBaseTypes()
                        .Any(t => t.ImplementsAny(ComparableInterfaces));
                    if (baseImplementsIComparable)
                    {
                        return;
                    }

                    var implementedComparableInterfaces = GetImplementedComparableInterfaces(classSymbol);
                    if (!implementedComparableInterfaces.Any())
                    {
                        return;
                    }

                    var classMembers = classSymbol.GetMembers().OfType<IMethodSymbol>();

                    var membersToOverride = GetMembersToOverride(classMembers).ToList();

                    if (membersToOverride.Any())
                    {
                        c.ReportDiagnostic(Diagnostic.Create(
                            Rule, 
                            classDeclaration.Identifier.GetLocation(),
                            string.Join(" or ", implementedComparableInterfaces),
                            string.Join(", ", membersToOverride)));
                    }
                },
                SyntaxKind.ClassDeclaration);
        }

        private static IEnumerable<string> GetImplementedComparableInterfaces(INamedTypeSymbol classSymbol)
        {
            return classSymbol
                .Interfaces
                .Where(i => i.OriginalDefinition.IsAny(ComparableInterfaces))
                .Select(GetClassNameOnly)
                .ToList();
        }

        private static string GetClassNameOnly(INamedTypeSymbol typeSymbol)
        {
            var fullName = typeSymbol.OriginalDefinition.ToString();

            return fullName
                .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Last();
        }

        private static IEnumerable<string> GetMembersToOverride(IEnumerable<IMethodSymbol> methods)
        {
            if (!methods.Any(IsObjectEquals))
            {
                yield return ObjectEquals;
            }

            var overridenOperators = methods
                .Where(m => m.MethodKind == MethodKind.UserDefinedOperator)
                .Select(m => m.Name);

            foreach (var op in RequiredOperators.Except(overridenOperators))
            {
                yield return OperatorNamesMap[op];
            }
        }

        private static bool IsObjectEquals(IMethodSymbol methodSymbol)
        {
            return methodSymbol.MethodKind == MethodKind.Ordinary &&
                methodSymbol.Name == ObjectEquals &&
                methodSymbol.Parameters.Length == 1 &&
                methodSymbol.Parameters[0].Type.Is(KnownType.System_Object) &&
                !methodSymbol.ReturnsVoid &&
                methodSymbol.ReturnType.Is(KnownType.System_Boolean);
        }
    }
}
