using Microsoft.EntityFrameworkCore;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Data
{
    using System.Linq.Expressions;
    using Entities;

    public class OrderRepository : IOrderRepository
    {
        private const string CreatedStatus = "Created";
        private const string CompletedStatus = "Completed";
        
        private readonly OrderContext _orderContext;

        public OrderRepository(OrderContext orderContext)
        {
            _orderContext = orderContext;
        }
        
        //abstracted select logic to an expression rather than duplicating select statement in GetOrdersByStatusAsync
        private static readonly Expression<Func<Order, OrderSummary>> OrderSummarySelector = x => new OrderSummary
        {
            Id = new Guid(x.Id),
            ResellerId = new Guid(x.ResellerId),
            CustomerId = new Guid(x.CustomerId),
            StatusId = new Guid(x.StatusId),
            StatusName = x.Status.Name,
            ItemCount = x.Items.Count,
            TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
            TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
            CreatedDate = x.CreatedDate
        };

        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
        {
            var orders = await _orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .Select(OrderSummarySelector)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return orders;
        }
        
        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName)
        {
            var normalisedStatus = statusName.Trim().ToLower();

            var orders = await _orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .Where(x => x.Status.Name.ToLower() == normalisedStatus)
                .Select(OrderSummarySelector)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return orders;
        }

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var orderIdBytes = orderId.ToByteArray();

            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
                .Select(x => new OrderDetail
                {
                    Id = new Guid(x.Id),
                    ResellerId = new Guid(x.ResellerId),
                    CustomerId = new Guid(x.CustomerId),
                    StatusId = new Guid(x.StatusId),
                    StatusName = x.Status.Name,
                    CreatedDate = x.CreatedDate,
                    TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                    TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                    Items = x.Items.Select(i => new Model.OrderItem
                    {
                        Id = new Guid(i.Id),
                        OrderId = new Guid(i.OrderId),
                        ServiceId = new Guid(i.ServiceId),
                        ServiceName = i.Service.Name,
                        ProductId = new Guid(i.ProductId),
                        ProductName = i.Product.Name,
                        UnitCost = i.Product.UnitCost,
                        UnitPrice = i.Product.UnitPrice,
                        TotalCost = i.Product.UnitCost * i.Quantity.Value,
                        TotalPrice = i.Product.UnitPrice * i.Quantity.Value,
                        Quantity = i.Quantity.Value
                    })
                }).SingleOrDefaultAsync();
            
            return order;
        }

        public async Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, string statusName)
        {
            var orderIdBytes = orderId.ToByteArray();
            
            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
                .SingleOrDefaultAsync();

            if (order == null)
            {
                return new UpdateOrderStatusResult { Outcome = UpdateOrderStatusOutcome.OrderNotFound };
            }

            var status = await GetStatusByNameAsync(statusName);

            if (status == null)
            {
                return new UpdateOrderStatusResult { Outcome = UpdateOrderStatusOutcome.InvalidStatus };
            }

            order.StatusId = status.Id;
            await _orderContext.SaveChangesAsync();

            return new UpdateOrderStatusResult { Outcome = UpdateOrderStatusOutcome.Success };
        }

        public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request)
        {
            var errors = new List<string>();
            var orderItems = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                var productIdBytes = item.ProductId.ToByteArray();
                var product = await _orderContext.OrderProduct
                    .Where(x => _orderContext.IsInMemoryDatabase()
                        ? x.Id.SequenceEqual(productIdBytes)
                        : x.Id == productIdBytes)
                    .Include(orderProduct => orderProduct.Service)
                    .SingleOrDefaultAsync();

                if (product == null)
                {
                    errors.Add($"Product {item.ProductId} not found");
                    continue;
                }
                
                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid().ToByteArray(),
                    ProductId = product.Id,
                    ServiceId = product.Service.Id,
                    Quantity = item.Quantity
                });
            }

            if (errors.Any())
            {
                return new CreateOrderResult()
                {
                    Success = false,
                    Errors = errors
                };
            }

            var status = await GetStatusByNameAsync(CreatedStatus);

            if (status == null)
            {
                errors.Add($"Default order status '{CreatedStatus}' is not configured."); 
            }
            
            if (errors.Any())
            {
                return new CreateOrderResult
                {
                    Success = false,
                    Errors = errors
                };
            }

            var order = new Order
            {
                Id = Guid.NewGuid().ToByteArray(),
                ResellerId = request.ResellerId.ToByteArray(),
                CustomerId = request.CustomerId.ToByteArray(),
                StatusId = status.Id,
                CreatedDate = DateTime.Now
            };

            foreach (var item in orderItems)
            {
                item.OrderId = order.Id;
            }
            
            _orderContext.Order.Add(order);
            _orderContext.OrderItem.AddRange(orderItems);
            
            await _orderContext.SaveChangesAsync();

            return new CreateOrderResult()
            {
                Success = true,
                OrderId = new Guid(order.Id)
            };

        }
        
        private async Task<OrderStatus> GetStatusByNameAsync(string statusName)
        {
            var normalizedStatus = statusName.Trim().ToLowerInvariant();

            return await _orderContext.OrderStatus
                .Where(x => x.Name.ToLower() == normalizedStatus)
                .SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitForCompletedOrdersAsync()
        {
            var completedOrders = await GetOrdersByStatusAsync(CompletedStatus);

            var monthlyProfits = new List<MonthlyProfit>();

            foreach (var order in completedOrders)
            {
                var year = order.CreatedDate.Year;
                var month = order.CreatedDate.Month;

                var monthlyProfit = monthlyProfits.FirstOrDefault(x => x.Year == year && x.Month == month);

                if (monthlyProfit == null)
                {
                    monthlyProfit = new MonthlyProfit
                    {
                        Year = year,
                        Month = month
                    };

                    monthlyProfits.Add(monthlyProfit);
                }

                monthlyProfit.TotalCost += order.TotalCost;
                monthlyProfit.TotalPrice += order.TotalPrice;
                monthlyProfit.Profit = monthlyProfit.TotalPrice - monthlyProfit.TotalCost;
            }

            return monthlyProfits
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();
        }
    }
}
