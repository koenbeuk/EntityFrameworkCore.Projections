using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using EntityFrameworkCore.Projectables.FunctionalTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EntityFrameworkCore.Projectables.FunctionalTests
{
    public class MemberBodyParameterValueTests
    {
        public class Order
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public List<OrderItem> OrderItem { get; set; } = new List<OrderItem>();

            [Projectable(UseMemberBody = nameof(GetOrderItemNameExpression), UseMemberBodyArguments = new object[]{ 1 } )]
            public string FirstOrderPropName => GetOrderItemName(1);


            [Projectable(UseMemberBody = nameof(GetOrderItemNameInnerExpression))]
            public string GetOrderItemName(int id)
                => OrderItem.Where(av => av.Id == id)
                    .Select(av => av.Name)
                    .FirstOrDefault() ?? throw new ArgumentException("Argument matching identifier not found");

            private static Expression<Func<Order, int, string>> GetOrderItemNameInnerExpression()
                => (@this, id) => @this.OrderItem
                    .Where(av => av.Id == id)
                    .Select(av => av.Name)
                    .FirstOrDefault() ?? string.Empty;

            public static Expression<Func<Order, int, string>> GetOrderItemNameExpression
                => (order, id) => order.GetOrderItemName(id);
        }

        public class OrderItem
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public int OrderId { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void UseBodyParameterValue()
        {
            //Arrange
            using var dbContext = new SampleBodyParamDbContext();

            // Act
            var query = dbContext
                .Set<Order>()
                .Include(a => a.OrderItem)
                .FirstOrDefault(d => d.FirstOrderPropName == "Order_1");

            // Assert
            Assert.NotNull(query);
            Assert.True(query!.FirstOrderPropName == "Order_1");
        }
    }
}
