using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Service
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderSummary>> GetOrdersAsync();

        Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName);
        
        Task<OrderDetail> GetOrderByIdAsync(Guid orderId);

        Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, string statusName);
        
        Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request);

        Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitForCompletedOrdersAsync();
    }
}
