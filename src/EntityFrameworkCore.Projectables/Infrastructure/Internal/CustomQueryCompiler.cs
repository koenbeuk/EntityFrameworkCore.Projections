using System.Linq.Expressions;
using EntityFrameworkCore.Projectables.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.Projectables.Infrastructure.Internal
{
    /// <summary>
    /// Foo
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Needed")]
    public sealed class CustomQueryCompiler : QueryCompiler
    {
        readonly IQueryCompiler _decoratedQueryCompiler;
        readonly ProjectableExpressionReplacer _projectableExpressionReplacer;

        public CustomQueryCompiler(IQueryCompiler decoratedQueryCompiler, IQueryContextFactory queryContextFactory, ICompiledQueryCache compiledQueryCache, ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator, IDatabase database, IDiagnosticsLogger<DbLoggerCategory.Query> logger, ICurrentDbContext currentContext, IEvaluatableExpressionFilter evaluatableExpressionFilter, IModel model) : base(queryContextFactory, compiledQueryCache, compiledQueryCacheKeyGenerator, database, logger, currentContext, evaluatableExpressionFilter, model)
        {
            _decoratedQueryCompiler = decoratedQueryCompiler;
            _projectableExpressionReplacer = new ProjectableExpressionReplacer(new ProjectionExpressionResolver());
        }

        public override Func<QueryContext, TResult> CreateCompiledAsyncQuery<TResult>(Expression query) 
            => _decoratedQueryCompiler.CreateCompiledAsyncQuery<TResult>(Expand(query));
        public override Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query) 
            => _decoratedQueryCompiler.CreateCompiledQuery<TResult>(Expand(query));
        public override TResult Execute<TResult>(Expression query)
            => _decoratedQueryCompiler.Execute<TResult>(Expand(query));
        public override TResult ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)
            => _decoratedQueryCompiler.ExecuteAsync<TResult>(Expand(query), cancellationToken);

        Expression Expand(Expression expression)
            => _projectableExpressionReplacer.Replace(expression);
    }
}
