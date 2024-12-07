using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityFrameworkCore.Projectables.Generator
{
    public static class ProjectableInterpreter
    {
        static IEnumerable<string> GetNestedInClassPath(ITypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ContainingType is not null)
            {
                foreach (var nestedInClassName in GetNestedInClassPath(namedTypeSymbol.ContainingType))
                {
                    yield return nestedInClassName;
                }
            }

            yield return namedTypeSymbol.Name;
        }

        public static ProjectableDescriptor? GetDescriptor(Compilation compilation, MemberDeclarationSyntax member, SourceProductionContext context)
        {
            var semanticModel = compilation.GetSemanticModel(member.SyntaxTree);
            var memberSymbol = semanticModel.GetDeclaredSymbol(member);

            if (memberSymbol is null)
            {
                return null;
            }

            var projectableAttributeTypeSymbol = compilation.GetTypeByMetadataName("EntityFrameworkCore.Projectables.ProjectableAttribute");

            var projectableAttributeClass = memberSymbol.GetAttributes()
                .Where(x => x.AttributeClass?.Name == "ProjectableAttribute")
                .FirstOrDefault();

            if (projectableAttributeClass is null || !SymbolEqualityComparer.Default.Equals(projectableAttributeClass.AttributeClass, projectableAttributeTypeSymbol))
            {
                return null;
            }

            var nullConditionalRewriteSupport = projectableAttributeClass.NamedArguments
                .Where(x => x.Key == "NullConditionalRewriteSupport")
                .Where(x => x.Value.Kind == TypedConstantKind.Enum)
                .Select(x => x.Value.Value)
                .Where(x => Enum.IsDefined(typeof(NullConditionalRewriteSupport), x))
                .Cast<NullConditionalRewriteSupport>()
                .FirstOrDefault();

            var useMemberBody = projectableAttributeClass.NamedArguments
                .Where(x => x.Key == "UseMemberBody")
                .Select(x => x.Value.Value)
                .OfType<string?>()
                .FirstOrDefault();

            var memberBody = member;

            if (useMemberBody is not null)
            {
                var comparer = SymbolEqualityComparer.Default;

                memberBody = memberSymbol.ContainingType.GetMembers(useMemberBody)
                    .Where(x =>
                    {
                        if (memberSymbol is IMethodSymbol symbolMethod &&
                            x is IMethodSymbol xMethod &&
                            comparer.Equals(symbolMethod.ReturnType, xMethod.ReturnType) &&
                            symbolMethod.TypeArguments.Length == xMethod.TypeArguments.Length &&
                            !symbolMethod.TypeArguments.Zip(xMethod.TypeArguments, (a, b) => !comparer.Equals(a, b)).Any())
                        {
                            return true;
                        }
                        else if (memberSymbol is IPropertySymbol symbolProperty &&
                            x is IPropertySymbol xProperty &&
                            comparer.Equals(symbolProperty.Type, xProperty.Type))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    })
                    .SelectMany(x => x.DeclaringSyntaxReferences)
                    .Select(x => x.GetSyntax())
                    .OfType<MemberDeclarationSyntax>()
                    .FirstOrDefault(x =>
                    {
                        if (x == null ||
                            x.SyntaxTree != member.SyntaxTree ||
                            x.Modifiers.Any(SyntaxKind.StaticKeyword) != member.Modifiers.Any(SyntaxKind.StaticKeyword))
                        {
                            return false;
                        }
                        else if (x is MethodDeclarationSyntax xMethod &&
                            xMethod.ExpressionBody is not null)
                        {
                            return true;
                        }
                        else if (x is PropertyDeclarationSyntax xProperty &&
                            xProperty.ExpressionBody is not null)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    });

                if (memberBody is null) return null;
            }

            var expressionSyntaxRewriter = new ExpressionSyntaxRewriter(memberSymbol.ContainingType, nullConditionalRewriteSupport, semanticModel, context);
            var declarationSyntaxRewriter = new DeclarationSyntaxRewriter(semanticModel);

            var descriptor = new ProjectableDescriptor {

                UsingDirectives = member.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>(),                    
                ClassName = memberSymbol.ContainingType.Name,
                ClassNamespace = memberSymbol.ContainingType.ContainingNamespace.IsGlobalNamespace ? null : memberSymbol.ContainingType.ContainingNamespace.ToDisplayString(),
                MemberName = memberSymbol.Name,
                NestedInClassNames = GetNestedInClassPath(memberSymbol.ContainingType),
                ParametersList = SyntaxFactory.ParameterList()
            };

            if (memberSymbol.ContainingType is INamedTypeSymbol { IsGenericType: true } containingNamedType)
            {
                descriptor.ClassTypeParameterList = SyntaxFactory.TypeParameterList();

                foreach (var additionalClassTypeParameter in containingNamedType.TypeParameters)
                {
                    descriptor.ClassTypeParameterList = descriptor.ClassTypeParameterList.AddParameters(
                        SyntaxFactory.TypeParameter(additionalClassTypeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    );
                    
                    // See https://github.com/dotnet/roslyn/blob/d7e010bbe5b1d37837417fc5e79ecb2fd9b7b487/src/VisualStudio/CSharp/Impl/ObjectBrowser/DescriptionBuilder.cs#L340
                    if (!additionalClassTypeParameter.ConstraintTypes.IsDefaultOrEmpty)
                    {
                        descriptor.ClassConstraintClauses ??= SyntaxFactory.List<TypeParameterConstraintClauseSyntax>();

                        var parameters = new List<TypeConstraintSyntax>();

                        if (additionalClassTypeParameter.HasReferenceTypeConstraint)
                        {
                            parameters.Add(MakeTypeConstraint("class"));
                        }

                        if (additionalClassTypeParameter.HasValueTypeConstraint)
                        {
                            parameters.Add(MakeTypeConstraint("struct"));
                        }

                        if (additionalClassTypeParameter.HasNotNullConstraint)
                        {
                            parameters.Add(MakeTypeConstraint("notnull"));
                        }
                        
                        parameters.AddRange(additionalClassTypeParameter
                            .ConstraintTypes
                            .Select(c => 
                                MakeTypeConstraint(c.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                            )
                        );

                        if (additionalClassTypeParameter.HasConstructorConstraint)
                        {
                            parameters.Add(MakeTypeConstraint("new()"));
                        }

                        descriptor.ClassConstraintClauses = descriptor.ClassConstraintClauses.Value.Add(
                            SyntaxFactory.TypeParameterConstraintClause(
                                SyntaxFactory.IdentifierName(additionalClassTypeParameter.Name),
                                SyntaxFactory.SeparatedList<TypeParameterConstraintSyntax>(parameters)
                            )
                        );
                    }
                }
            }

            if (!member.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                descriptor.ParametersList = descriptor.ParametersList.AddParameters(
                    SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier("@this")
                    )
                    .WithType(
                        SyntaxFactory.ParseTypeName(
                            memberSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        )
                    )
                );
            }

            var methodSymbol = memberSymbol as IMethodSymbol;

            if (methodSymbol is { IsExtensionMethod: true })
            {
                var targetTypeSymbol = methodSymbol.Parameters.First().Type;
                descriptor.TargetClassNamespace = targetTypeSymbol.ContainingNamespace.IsGlobalNamespace ? null : targetTypeSymbol.ContainingNamespace.ToDisplayString();
                descriptor.TargetNestedInClassNames = GetNestedInClassPath(targetTypeSymbol);
            }
            else
            {
                descriptor.TargetClassNamespace = descriptor.ClassNamespace;
                descriptor.TargetNestedInClassNames = descriptor.NestedInClassNames;
            }

            if (memberBody is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                if (methodDeclarationSyntax.ExpressionBody is null)
                {
                    var diagnostic = Diagnostic.Create(Diagnostics.RequiresExpressionBodyDefinition, methodDeclarationSyntax.GetLocation(), memberSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                    return null;
                }

                var returnType = declarationSyntaxRewriter.Visit(methodDeclarationSyntax.ReturnType);

                descriptor.ReturnTypeName = returnType.ToString();
                descriptor.ExpressionBody = (ExpressionSyntax)expressionSyntaxRewriter.Visit(methodDeclarationSyntax.ExpressionBody.Expression);
                foreach (var additionalParameter in ((ParameterListSyntax)declarationSyntaxRewriter.Visit(methodDeclarationSyntax.ParameterList)).Parameters)
                {
                    descriptor.ParametersList = descriptor.ParametersList.AddParameters(additionalParameter);
                }

                if (methodDeclarationSyntax.TypeParameterList is not null)
                {
                    descriptor.TypeParameterList = SyntaxFactory.TypeParameterList();
                    foreach (var additionalTypeParameter in ((TypeParameterListSyntax)declarationSyntaxRewriter.Visit(methodDeclarationSyntax.TypeParameterList)).Parameters)
                    {
                        descriptor.TypeParameterList = descriptor.TypeParameterList.AddParameters(additionalTypeParameter);
                    }
                }

                if (methodDeclarationSyntax.ConstraintClauses.Any())
                {
                    descriptor.ConstraintClauses = SyntaxFactory.List(
                        methodDeclarationSyntax
                            .ConstraintClauses
                            .Select(x => (TypeParameterConstraintClauseSyntax)declarationSyntaxRewriter.Visit(x))
                        );
                }
            }
            else if (memberBody is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                var expressionBody = propertyDeclarationSyntax.ExpressionBody;
                if (expressionBody is null)
                {
                    //try to get from getter
                    var getter = propertyDeclarationSyntax.AccessorList?.Accessors.FirstOrDefault(x => x.Kind() == SyntaxKind.GetAccessorDeclaration);
                    expressionBody = getter?.ExpressionBody;
                }
                if (expressionBody is null)
                {
                    var diagnostic = Diagnostic.Create(Diagnostics.RequiresExpressionBodyDefinition, propertyDeclarationSyntax.GetLocation(), memberSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                    return null;
                }

                var returnType = declarationSyntaxRewriter.Visit(propertyDeclarationSyntax.Type);

                descriptor.ReturnTypeName = returnType.ToString();
                descriptor.ExpressionBody = (ExpressionSyntax)expressionSyntaxRewriter.Visit(expressionBody.Expression);
            }
            else
            {
                return null;
            }

            return descriptor;
        }

        private static TypeConstraintSyntax MakeTypeConstraint(string constraint) => SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName(constraint));
    }
}
