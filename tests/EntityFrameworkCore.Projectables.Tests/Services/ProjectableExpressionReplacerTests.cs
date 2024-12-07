using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EntityFrameworkCore.Projectables.Services;
using Xunit;

namespace EntityFrameworkCore.Projectables.Tests.Services
{
    public class ProjectableExpressionReplacerTests
    {
        public class ProjectableExpressionResolverStub : IProjectionExpressionResolver
        {
            readonly Func<MemberInfo, LambdaExpression> _implementation;

            public ProjectableExpressionResolverStub(Func<MemberInfo, LambdaExpression> implementation)
            {
                _implementation = implementation;
            }

            public LambdaExpression FindGeneratedExpression(MemberInfo projectableMemberInfo) => _implementation(projectableMemberInfo);
        }

        class Entity
        {
            public int Id { get; set; }

            [Projectable]
            public int SimpleProperty => 0;

            [Projectable]
            public int SimpleMethod() => 0;

            [Projectable]
            public int SimpleMethodWithArguments(int a, object b) => 0;

            [Projectable]
            public int SimpleStatefullProperty => Id;

            [Projectable]
            public int SimpleStatefullMethod() => Id;

            [Projectable]
            public static int SimpleStaticMethod() => 0;

            [Projectable]
            public static int SimpleStaticMethodWithArguments(int a, Entity b) => 0;
        }

        [Fact]
        public void VisitMember_SimpleProperty()
        {
            Expression<Func<Entity, int>> input = x => x.SimpleProperty;
            Expression<Func<Entity, int>> expected = x => 0;

            var resolver = new ProjectableExpressionResolverStub(
                x => expected
            );
            var subject = new ProjectableExpressionReplacer(resolver);

            var actual = subject.Visit(input);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void VisitMember_SimpleMethod()
        {
            Expression<Func<Entity, int>> input = x => x.SimpleMethod();
            Expression<Func<Entity, int>> expected = x => 0;

            var resolver = new ProjectableExpressionResolverStub(
                x => expected
            );
            var subject = new ProjectableExpressionReplacer(resolver);

            var actual = subject.Visit(input);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void VisitMember_SimpleMethodWithArguments()
        {
            Expression<Func<Entity, int>> input = x => x.SimpleMethodWithArguments(1, true);
            Expression<Func<Entity, int>> expected = x => 0;

            var resolver = new ProjectableExpressionResolverStub(
                x => expected
            );
            var subject = new ProjectableExpressionReplacer(resolver);

            var actual = subject.Visit(input);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void VisitMember_SimpleStatefullProperty()
        {
            Expression<Func<Entity, int>> input = x => x.SimpleStatefullProperty;
            Expression<Func<Entity, int>> expected = x => x.Id;

            var resolver = new ProjectableExpressionResolverStub(
                x => expected
            );
            var subject = new ProjectableExpressionReplacer(resolver);

            var actual = subject.Visit(input);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void VisitMember_SimpleStatefullMethod()
        {
            Expression<Func<Entity, int>> input = x => x.SimpleStatefullMethod();
            Expression<Func<Entity, int>> expected = x => x.Id;

            var resolver = new ProjectableExpressionResolverStub(
                x => expected
            );
            var subject = new ProjectableExpressionReplacer(resolver);

            var actual = subject.Visit(input);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void VisitMember_SimpleStaticMethod()
        {
            Expression<Func<Entity, int>> input = x => Entity.SimpleStaticMethod();
            Expression<Func<Entity, int>> expected = x => 0;

            var resolver = new ProjectableExpressionResolverStub(
                x => expected
            );
            var subject = new ProjectableExpressionReplacer(resolver);

            var actual = subject.Visit(input);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void VisitMember_SimpleStaticMethodWithArguments()
        {
            Expression<Func<Entity, int>> input = x => Entity.SimpleStaticMethodWithArguments(0, x);
            Expression<Func<Entity, int>> expected = x => 0;

            var resolver = new ProjectableExpressionResolverStub(
                x => expected
            );
            var subject = new ProjectableExpressionReplacer(resolver);

            var actual = subject.Visit(input);

            Assert.Equal(expected.ToString(), actual.ToString());
        }
    }
}
