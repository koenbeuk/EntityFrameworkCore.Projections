using System.Collections.Generic;
using EntityFrameworkCore.Projectables.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Order = EntityFrameworkCore.Projectables.FunctionalTests.MemberBodyParameterValueTests.Order;
using OrderItem = EntityFrameworkCore.Projectables.FunctionalTests.MemberBodyParameterValueTests.OrderItem;

namespace EntityFrameworkCore.Projectables.FunctionalTests.Helpers
{
    public class SampleBodyParamDbContext : DbContext
    {
        readonly CompatibilityMode _compatibilityMode;

        public SampleBodyParamDbContext(CompatibilityMode compatibilityMode = CompatibilityMode.Full)
        {
            _compatibilityMode = compatibilityMode;

            var _orders = new List<Order>() {
                new() {
                    Id = 1, 
                   
                },
                new() {
                    Id = 2,
                   
                }
            };

            var _orders_items = new List<OrderItem>() {
                new() {
                    Id = 1,
                    OrderId = 1,
                    Name = "Order_1"
                },
                new() {
                    Id = 2,
                    OrderId = 1,
                    Name = "Order_2"
                },
                new() {
                    Id = 3,
                    OrderId = 2,
                    Name = "Order_3"
                },
                new() {
                    Id = 4,
                    OrderId = 2,
                    Name = "Order_4"
                },
            };

            Order!.AddRange(_orders);
            OrderItem!.AddRange(_orders_items);
            SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("TestDb");
            optionsBuilder.UseProjectables(options => {
                options.CompatibilityMode(_compatibilityMode); // Needed by our ComplexModelTests
            });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                        .HasKey(x => x.Id);
            modelBuilder.Entity<OrderItem>().HasKey(x => x.Id);
           
        }

        public DbSet<Order>? Order { get; set; }
        public DbSet<OrderItem>? OrderItem { get; set; }
    }
}
