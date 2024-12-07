using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Projectables.Generator.Tests
{
    [UsesVerify]
    public class ProjectionExpressionGeneratorTests
    {
        readonly ITestOutputHelper _testOutputHelper;

        public ProjectionExpressionGeneratorTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void EmtpyCode_Noop()
        {
            var compilation = CreateCompilation(@"
class C { }
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Empty(result.GeneratedTrees);
        }

        [Fact]
        public Task SimpleProjectableMethod()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        [Projectable]
        public int Foo() => 1;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task SimpleProjectableProperty()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        [Projectable]
        public int Foo => 1;
    }
}
");
            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task MinimalProjectableComputedProperty()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        public int Bar { get; set; }

        [Projectable]
        public int Foo => Bar;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task SimpleProjectableComputedProperty()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        public int Bar { get; set; }

        [Projectable]
        public int Foo => Bar + 1;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task SimpleProjectableComputedPropertyWithSetter()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        public int Bar { get; set; }

        [Projectable]
        public int Foo 
        { 
            get => Bar;
            set => Bar = value;
        }
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task SimpleProjectableComputedInNestedClassProperty()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        class D {
            public int Bar { get; set; }

            [Projectable]
            public int Foo => Bar + 1;
        }
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ProjectableComputedPropertyUsingThis()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        public int Bar { get; set; }

        [Projectable]
        public int Foo => this.Bar;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ProjectableComputedPropertyMethod()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        public int Bar() => 1;

        [Projectable]
        public int Foo => Bar();
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }


        [Fact]
        public Task MoreComplexProjectableComputedProperty()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        public int Bar { get; set; }

        [Projectable]
        public int Foo => Bar + this.Bar + Bar;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ArgumentlessProjectableComputedMethod()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        [Projectable]
        public int Foo() => 0;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ProjectableComputedMethodWithSingleArgument()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        [Projectable]
        public int Foo(int i) => i;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ProjectableComputedMethodWithMultipleArguments()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        [Projectable]
        public int Foo(int a, string b, object d) => a;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ProjectablePropertyToNavigationalProperty()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class D { }
    
    class C {
        public System.Collections.Generic.List<D> Dees { get; set; }

        [Projectable]
        public D Foo => Dees.First();
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task TypesInBodyGetsFullyQualified()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class D { }
    
    class C {
        public System.Collections.Generic.List<D> Dees { get; set; }

        [Projectable]
        public int Foo => Dees.OfType<D>().Count();
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task IsOperator()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class A {
        [Projectable] 
        public bool IsB => this is B;
    }
    
    class B : A {
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ProjectableExtensionMethod()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class D { }
    
    static class C {
        [Projectable]
        public static int Foo(this D d) => 1;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ProjectableExtensionMethod2()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;
namespace Foo {
    static class C {
        [Projectable]
        public static int Foo(this int i) => i;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }


        [Fact]
        public Task ProjectableExtensionMethod3()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;
namespace Foo {
    static class C {
        [Projectable]
        public static int Foo1(this int i) => i;

        [Projectable]
        public static int Foo2(this int i) => i.Foo1();
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(2, result.GeneratedTrees.Length);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ProjectableExtensionMethod4()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;
namespace Foo {
    static class C {
        [Projectable]
        public static object Foo1(this object i) => i.Foo1();
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public void BlockBodiedMethod_RaisesDiagnostics()
        {
            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;
namespace Foo {
    class C {
        [Projectable]
        public int Foo() 
        {
            return 1;
        }
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Single(result.Diagnostics);
        }

        [Fact]
        public Task NullableReferenceTypesAreBeingEliminated()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

#nullable enable

namespace Foo {
    static class C {
        [Projectable]
        public static object? NextFoo(this object? unusedArgument, int? nullablePrimitiveArgument) => null;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }


        [Fact]
        public Task GenericNullableReferenceTypesAreBeingEliminated()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Collections.Generic;
using System.Linq;
using EntityFrameworkCore.Projectables;

#nullable enable

namespace Foo {
    static class C {
        [Projectable]
        public static List<object?> NextFoo(this List<object?> input, List<int?> nullablePrimitiveArgument) => input;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NullableReferenceTypeCastOperatorGetsEliminated()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Collections.Generic;
using System.Linq;
using EntityFrameworkCore.Projectables;

#nullable enable

namespace Foo {
    static class C {
        [Projectable]
        public static string? NullableReferenceType(object? input) => (string?)input;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NullableValueCastOperatorsPersist()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Collections.Generic;
using System.Linq;
using EntityFrameworkCore.Projectables;

#nullable enable

namespace Foo {
    static class C {
        [Projectable]        
        public static int? NullableValueType(object? input) => (int?)input;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }


        [Fact]
        public void NullableMemberBinding_WithoutSupport_IsBeingReported()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class C {
        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.None)]
        public static int? GetLength(this string input) => input?.Length;
    }
}
");
            var result = RunGenerator(compilation);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("EFP0002", diagnostic.Id);
        }

        [Fact]
        public void NullableMemberBinding_UndefinedSupport_IsBeingReported()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class C {
        [Projectable]
        public static int? GetLength(this string input) => input?.Length;
    }
}
");
            var result = RunGenerator(compilation);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("EFP0002", diagnostic.Id);
        }


        [Fact]
        public void MultiLevelNullableMemberBinding_UndefinedSupport_IsBeingReported()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
        public record Address
        {
            public int Id { get; set; }
            public string? Country { get; set; }
        }

        public record Party
        {
            public int Id { get; set; }

            public Address? Address { get; set; }
        }

        public record Entity
        {
            public int Id { get; set; }

            public Party? Left { get; set; }
            public Party? Right { get; set; }

            [Projectable]
            public bool IsSameCountry => Left?.Address?.Country == Right?.Address?.Country;
        }
}
");
            var result = RunGenerator(compilation);

            Assert.All(result.Diagnostics, diagnostic => {
                Assert.Equal("EFP0002", diagnostic.Id);
            });
        }

        [Fact]
        public Task NullableMemberBinding_WithIgnoreSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class C {
        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Ignore)]
        public static int? GetLength(this string input) => input?.Length;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NullableMemberBinding_WithRewriteSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class C {
        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Rewrite)]
        public static int? GetLength(this string input) => input?.Length;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NullableSimpleElementBinding_WithIgnoreSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class C {
        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Ignore)]
        public static char? GetFirst(this string input) => input?[0];
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }
        
        [Fact]
        public Task StringInterpolationWithStaticCall_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class MyExtensions {
        public static string ToDateString(this DateTime date) => date.ToString(""dd/MM/yyyy"");
    }

    class C {
        public DateTime? ValidationDate { get; set; }

        [Projectable]
        public string Status => ValidationDate != null ? $""Validation date : ({ValidationDate.Value.ToDateString()})"" : """";
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }
        
        [Fact]
        public Task StringInterpolationWithParenthesis_NoParenthesisAdded()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class MyExtensions {
        public static string ToDateString(this DateTime date) => date.ToString(""dd/MM/yyyy"");
    }

    class C {
        public DateTime? ValidationDate { get; set; }

        [Projectable]
        public string Status => ValidationDate != null ? $""Validation date : ({(ValidationDate.Value.ToDateString())})"" : """";
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NullableSimpleElementBinding_WithRewriteSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class C {
        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Rewrite)]
        public static char? GetFirst(this string input) => input?[0];
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task BooleanSimpleTernary_WithRewriteSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class C {
        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Rewrite)]
        public static bool Test(this object? x) => x?.Equals(4) == false;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }



        [Fact]
        public Task NullableElementBinding_WithIgnoreSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class C {
        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Ignore)]
        public static string? GetFirst(this string input) => input?[0].ToString();
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
         public Task NullableElementBinding_WithRewriteSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using EntityFrameworkCore.Projectables;

namespace Foo {
    static class C {
        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Rewrite)]
        public static string? GetFirst(this string input) => input?[0].ToString();
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NullableElementAndMemberBinding_WithIgnoreSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public static class EntityExtensions
    {
        public record Entity
        {
            public int Id { get; set; }
            public List<Entity>? RelatedEntities { get; set; }
        }

        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Ignore)]
        public static Entity GetFirstRelatedIgnoreNulls(this Entity entity)
            => entity?.RelatedEntities?[0];
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NullableElementAndMemberBinding_WithRewriteSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public static class EntityExtensions
    {
        public record Entity
        {
            public int Id { get; set; }
            public List<Entity>? RelatedEntities { get; set; }
        }

        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Rewrite)]
        public static Entity GetFirstRelatedIgnoreNulls(this Entity entity)
            => entity?.RelatedEntities?[0];
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NullableParameters_WithRewriteSupport_IsBeingRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public static class EntityExtensions
    {
        public record Entity
        {
            public int Id { get; set; }
            public string? FullName { get; set; }
        }

        [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Rewrite)]
        public static string GetFirstName(this Entity entity)
            => entity.FullName?.Substring(entity.FullName?.IndexOf(' ') ?? 0);
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task GenericMethods_AreRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public static class EntityExtensions
    {
        public record Entity
        {
            public int Id { get; set; }
            public string? FullName { get; set; }
        }

        [Projectable]
        public static string EnforceString<T>(T value) where T : unmanaged
            => value.ToString();
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task GenericClassesWithContraints_AreRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public class TypedObject<TEnum> where TEnum : struct, System.Enum
    {
        public TEnum SomeProp { get; set; }
    }

    public abstract class Entity<T, TEnum>where T : TypedObject<TEnum> where TEnum : struct, System.Enum 
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public T SomeSubobject { get; set; }

        [Projectable]
        public string FullName => $""{FirstName} {LastName} {SomeSubobject.SomeProp}"";
    }
}
");
            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task GenericClassesWithTypeContraints_AreRewritten()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public abstract class Entity<T> where T : notnull
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public T SomeSubobject { get; set; }

        [Projectable]
        public string FullName => $""{FirstName} {LastName} {SomeSubobject}"";
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            // Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task DeclarationTypeNamesAreGettingFullyQualified()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public static class EntityExtensions
    {
        public record Entity
        {
            public int Id { get; set; }
            public string? FullName { get; set; }

            [Projectable]
            public static Entity Something(Entity entity)
                => entity;
        }
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task MixPrimaryConstructorAndProperties()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public static class EntityExtensions
    {
        public record Entity(int Id)
        {
            public int Id { get; set; }
            public string? FullName { get; set; }

            [Projectable]
            public static Entity Something(Entity entity)
                => new Entity(entity.Id) {
                    FullName = entity.FullName
                };
        }
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task InheritedMembers()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public class Foo {
        public int Id { get; set; }
    }

    public class Bar : Foo {
        [Projectable]
        public int ProjectedId => Id;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task StaticMembers()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public class Foo {
        public static int Bar { get; set; }

        public int Id { get; set; }
  
        [Projectable]
        public int IdWithBar() => Id + Bar;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task StaticMembers2()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public static class Constants {
        public static readonly int Bar  = 1;
    }

    public class Foo {
        public int Id { get; set; }
  
        [Projectable]
        public int IdWithBar() => Id + Constants.Bar;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }


        [Fact]
        public Task StaticMethodWithNoParameters()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

public static class Foo {
    [Projectable]
    public static int Zero() => 0;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task StaticMethodWithParameters()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

public static class Foo {
    [Projectable]
    public static int Zero(int x) => 0;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ConstMember()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public class Foo {
        public const int Bar = 1;

        public int Id { get; set; }
  
        [Projectable]
        public int IdWithBar() => Id + Bar;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ConstMember2()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public static class Constants {
        public const int Bar  = 1;
    }

    public class Foo {
        public int Id { get; set; }
  
        [Projectable]
        public int IdWithBar() => Id + Constants.Bar;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ConstMember3()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foo {
    public class Foo {
        public const int Bar = 1;

        public int Id { get; set; }
  
        [Projectable]
        public int IdWithBar() => Id + Foo.Bar;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task RelationalProperty()
        {
            var compilation = CreateCompilation(@"
using System;
using System.Linq;
using System.Collections.Generic;
using EntityFrameworkCore.Projectables;

namespace Foos {
    public class Foo {
        public int Id { get; set; }
    }

    public class Bar {
        public Foo Foo { get; set; }

        [Projectable]
        public int FooId => Foo.Id;
    }
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task EnumAccessor()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

public enum SomeFlag
{
    Foo
}

public static class SomeExtensions
{
    [Projectable]
    public static bool Test(this SomeFlag f) => f == SomeFlag.Foo;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task Cast()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

namespace Projectables.Repro;

public class SuperEntity : SomeEntity
{
    public string Superpower { get; set; }
}

public class SomeEntity
{
    public int Id { get; set; }
}

public static class SomeExtensions
{
    [Projectable]
    public static string AsSomeResult(this SomeEntity e) => ((SuperEntity)e).Superpower;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NavigationProperties()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;
using System.Collections.Generic;

namespace Projectables.Repro;

public class SomeEntity
{
    public int Id { get; set; }

    public SomeEntity Parent { get; set; }

    public ICollection<SomeEntity> Children { get; set; }

    [Projectable]
    public ICollection<SomeEntity> RootChildren =>
        Parent != null ? Parent.RootChildren : Children;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task FooOrBar()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;
using System.Collections.Generic;

namespace Projectables.Repro;

public class SomeEntity
{
    public int Id { get; set; }

    public string Foo { get; set; }

    public string Bar { get; set; }

    [Projectable]
    public string FooOrBar =>
        Foo != null ? Foo : Bar;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task BaseMemberExplicitReference()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

namespace Projectables.Repro;

class Base 
{
    public string Foo { get; set; }
}

class Derived : Base
{
    [Projectable]
    public string Bar => base.Foo;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task BaseMemberImplicitReference()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

namespace Projectables.Repro;

class Base 
{
    public string Foo { get; set; }
}

class Derived : Base
{
    [Projectable]
    public string Bar => Foo;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task BaseMethodExplicitReference()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

namespace Projectables.Repro;

class Base 
{
    public string Foo() => """";
}

class Derived : Base
{
    [Projectable]
    public string Bar => base.Foo();
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task BaseMethorImplicitReference()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

namespace Projectables.Repro;

class Base 
{
    public string Foo() => """";
}

class Derived : Base
{
    [Projectable]
    public string Bar => Foo();
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task DefaultValuesGetRemoved()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

class Foo {
    [Projectable]
    public int Calculate(int i = 0) => i;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task ParamsModifiedGetsRemoved()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

class Foo {
    [Projectable]
    public int First(params int[] all) => all[0];
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task RequiredNamespace()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

namespace One {
    static class IntExtensions {
        public static int AddOne(this int i) => i + 1;    
    }
}

namespace One.Two {
    class Bar {
        [Projectable]
        public int Method() => 1.AddOne();
    }   
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task NullConditionalNullCoalesceTypeConversion()
        {
            // issue: https://github.com/koenbeuk/EntityFrameworkCore.Projectables/issues/48

            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

class Foo {
    public int? FancyNumber { get; set; }

    [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Rewrite)]
    public static int SomeNumber(Foo fancyClass) => fancyClass?.FancyNumber ?? 3;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task SwitchExpression()
        {
            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

class Foo {
    public int? FancyNumber { get; set; }

    [Projectable(NullConditionalRewriteSupport = NullConditionalRewriteSupport.Rewrite)]
    public int SomeNumber(int input) => input switch {
            1 => 2,
            3 => 4,
            4 when FancyNumber == 12 => 48,
            _ => 1000,
        };
    }
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task GenericTypes()
        {
            // issue: https://github.com/koenbeuk/EntityFrameworkCore.Projectables/issues/48

            var compilation = CreateCompilation(@"
using EntityFrameworkCore.Projectables;

class EntiyBase<TId> {
    [Projectable]
    public static TId GetId() => default;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        [Fact]
        public Task GenericTypesWithConstraints()
        {
            // issue: https://github.com/koenbeuk/EntityFrameworkCore.Projectables/issues/48

            var compilation = CreateCompilation(@"
using System;
using EntityFrameworkCore.Projectables;

class EntityBase<TId> where TId : ICloneable, new() {
    [Projectable]
    public static TId GetId() => default;
}
");

            var result = RunGenerator(compilation);

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedTrees);

            return Verifier.Verify(result.GeneratedTrees[0].ToString());
        }

        #region Helpers

        Compilation CreateCompilation([StringSyntax("c#")]string source, bool expectedToCompile = true)
        {
            var references = Basic.Reference.Assemblies.Net80.References.All.ToList();
            references.Add(MetadataReference.CreateFromFile(typeof(ProjectableAttribute).Assembly.Location));

            var compilation = CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

#if DEBUG

            if (expectedToCompile)
            {
                var compilationDiagnostics = compilation.GetDiagnostics();

                if (!compilationDiagnostics.IsEmpty)
                {
                    _testOutputHelper.WriteLine($"Original compilation diagnostics produced:");

                    foreach (var diagnostic in compilationDiagnostics)
                    {
                        _testOutputHelper.WriteLine($" > " + diagnostic.ToString());
                    }

                    if (compilationDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
                    {
                        Debug.Fail("Compilation diagnostics produced");
                    }
                }
            }
#endif

            return compilation;
        }

        private GeneratorDriverRunResult RunGenerator(Compilation compilation)
        {
            _testOutputHelper.WriteLine("Running generator and updating compilation...");

            var subject = new ProjectionExpressionGenerator();
            var driver = CSharpGeneratorDriver
                .Create(subject)
                .RunGenerators(compilation);

            var result = driver.GetRunResult();

            if (result.Diagnostics.IsEmpty)
            {
                _testOutputHelper.WriteLine("Run did not produce diagnostics");
            }
            else
            {
                _testOutputHelper.WriteLine($"Diagnostics produced:");

                foreach (var diagnostic in result.Diagnostics)
                {
                    _testOutputHelper.WriteLine($" > " + diagnostic.ToString());
                }
            }

            foreach (var newSyntaxTree in result.GeneratedTrees)
            {
                _testOutputHelper.WriteLine($"Produced syntax tree with path produced: {newSyntaxTree.FilePath}");
                _testOutputHelper.WriteLine(newSyntaxTree.GetText().ToString());
            }

            return driver.GetRunResult();
        }

        #endregion
    }
}
