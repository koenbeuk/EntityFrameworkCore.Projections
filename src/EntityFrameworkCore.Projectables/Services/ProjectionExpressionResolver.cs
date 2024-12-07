using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EntityFrameworkCore.Projectables.Extensions;

namespace EntityFrameworkCore.Projectables.Services
{
    public sealed class ProjectionExpressionResolver : IProjectionExpressionResolver
    {
        public LambdaExpression FindGeneratedExpression(MemberInfo projectableMemberInfo)
        {
            var projectableAttribute = projectableMemberInfo.GetCustomAttribute<ProjectableAttribute>()
                ?? throw new InvalidOperationException("Expected member to have a Projectable attribute. None found");

            var expression = projectableAttribute.UseMemberBody is null? GetExpressionFromGeneratedType(projectableMemberInfo): null;

            if (expression is null && projectableAttribute.UseMemberBody is not null && projectableAttribute.UseMemberBodyArguments is null)
            {
                expression = GetExpressionFromGeneratedType(projectableMemberInfo, true, projectableAttribute.UseMemberBody);
            }
            if (expression is null && projectableAttribute.UseMemberBody is not null)
            {
                expression = GetExpressionFromMemberBody(projectableMemberInfo, projectableAttribute.UseMemberBody, projectableAttribute.UseMemberBodyArguments);
            }
            if (expression is null)
            {
                var declaringType = projectableMemberInfo.DeclaringType ?? throw new InvalidOperationException("Expected a valid type here");
                var fullName = string.Join(".", Enumerable.Empty<string>()
                    .Concat(new[] { declaringType.Namespace })
                    .Concat(declaringType.GetNestedTypePath().Select(x => x.Name))
                    .Concat(new[] { projectableMemberInfo.Name }));

                throw new InvalidOperationException($"Unable to resolve generated expression for {fullName}.");
            }

            return expression;

            static LambdaExpression? GetExpressionFromMemberBody(MemberInfo projectableMemberInfo, string memberName, object[]? memberParameters)
            {
                var declaringType = projectableMemberInfo.DeclaringType ?? throw new InvalidOperationException("Expected a valid type here");

                var exprProperty = declaringType.GetProperty(memberName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                   ?? declaringType.BaseType?.GetProperty(memberName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var lambda = exprProperty?.GetValue(null) as LambdaExpression;

                if (lambda is not null)
                {

                    if (projectableMemberInfo is PropertyInfo property && (lambda.Parameters.Count ==
                                                                           (1 + (memberParameters?.Length ?? 0)))
                                                                       && (lambda.Parameters[0].Type == declaringType ||
                                                                           lambda.Parameters[0].Type ==
                                                                           declaringType.BaseType)
                                                                       && lambda.ReturnType == property.PropertyType
                                                                       && (memberParameters?.Any() != true
                                                                           || lambda.Parameters.Skip(1)
                                                                               .Select((Parameter, Index) =>
                                                                                   new { Parameter, Index })
                                                                               .All(p => p.Parameter.Type ==
                                                                                   memberParameters[p.Index]
                                                                                       .GetType())))


                    {
                        return lambda;
                    }
                    else if (projectableMemberInfo is MethodInfo method &&
                        lambda.Parameters.Count == method.GetParameters().Length + 1 &&
                        lambda.Parameters.Last().Type == declaringType &&
                        !lambda.Parameters.Zip(method.GetParameters(), (a, b) => a.Type != b.ParameterType).Any())
                    {
                        return lambda;
                    }
                }
                return null;
            }

            static LambdaExpression? GetExpressionFromGeneratedType(MemberInfo projectableMemberInfo, bool useLocalType = false, string methodName = "Expression")
            {
                var declaringType = projectableMemberInfo.DeclaringType ?? throw new InvalidOperationException("Expected a valid type here");
                var generatedContainingTypeName = ProjectionExpressionClassNameGenerator.GenerateFullName(declaringType.Namespace, declaringType.GetNestedTypePath().Select(x => x.Name), projectableMemberInfo.Name);
                var expressionFactoryType = !useLocalType
                                            ? declaringType.Assembly.GetType(generatedContainingTypeName)
                                            : declaringType;

                if (expressionFactoryType is not null)
                {
                    if (expressionFactoryType.IsGenericTypeDefinition)
                    {
                        expressionFactoryType = expressionFactoryType.MakeGenericType(declaringType.GenericTypeArguments);
                    }

                    var expressionFactoryMethod = expressionFactoryType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

                    var methodGenericArguments = projectableMemberInfo switch {
                        MethodInfo methodInfo => methodInfo.GetGenericArguments(),
                        _ => null
                    };

                    if (expressionFactoryMethod is not null)
                    {
                        if (methodGenericArguments is { Length: > 0 })
                        {
                            expressionFactoryMethod = expressionFactoryMethod.MakeGenericMethod(methodGenericArguments);
                        }

                        return expressionFactoryMethod.Invoke(null, null) as LambdaExpression ?? throw new InvalidOperationException("Expected lambda");
                    }
                }
                return null;
            }
        }
    }
}
