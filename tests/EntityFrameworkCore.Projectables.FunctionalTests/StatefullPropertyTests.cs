using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityFrameworkCore.Projectables.FunctionalTests.Helpers;
using Microsoft.EntityFrameworkCore;
using ScenarioTests;
using VerifyXunit;
using Xunit;

namespace EntityFrameworkCore.Projectables.FunctionalTests
{
    [UsesVerify]
    public class StatefullPropertyTests
    {
        public record Entity
        {
            public int Id { get; set; }
            
            [Projectable]
            public int Computed1 => Id;

            [Projectable]
            public int Computed2 => Id * 2;

            [Projectable]
            public int Alias
            {
                get => Id;
                set => Id = value;
            }
        }

        [Fact]
        public Task FilterOnProjectableProperty()
        {
            using var dbContext = new SampleDbContext<Entity>();

            var query = dbContext.Set<Entity>()
                .Where(x => x.Computed1 == 1);

            return Verifier.Verify(query.ToQueryString());
        }

        [Fact]
        public Task SelectProjectableProperty()
        {
            using var dbContext = new SampleDbContext<Entity>();

            var query = dbContext.Set<Entity>()
                .Select(x => x.Computed1);

            return Verifier.Verify(query.ToQueryString());
        }

        [Fact]
        public Task FilterOnComplexProjectableProperty()
        {
            using var dbContext = new SampleDbContext<Entity>();

            var query = dbContext.Set<Entity>()
                .Where(x => x.Computed2 == 2);

            return Verifier.Verify(query.ToQueryString());
        }

        [Fact]
        public Task SelectComplexProjectableProperty()
        {
            using var dbContext = new SampleDbContext<Entity>();

            var query = dbContext.Set<Entity>()
                .Select(x => x.Computed2);

            return Verifier.Verify(query.ToQueryString());
        }

        [Fact]
        public Task CombineSelectProjectableProperties()
        {
            using var dbContext = new SampleDbContext<Entity>();

            var query = dbContext.Set<Entity>()
                .Select(x => x.Computed1 + x.Computed2);

            return Verifier.Verify(query.ToQueryString());
        }


        [Fact]
        public Task FilterOnAliasProperty()
        {
            using var dbContext = new SampleDbContext<Entity>();

            var query = dbContext.Set<Entity>()
                .Where(x => x.Alias == 1);

            return Verifier.Verify(query.ToQueryString());
        }

        [Fact]
        public Task SelectAliasProperty()
        {
            using var dbContext = new SampleDbContext<Entity>();

            var query = dbContext.Set<Entity>()
                .Select(x => x.Alias);

            return Verifier.Verify(query.ToQueryString());
        }
    }
}
