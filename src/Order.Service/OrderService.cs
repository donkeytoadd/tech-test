using FluentValidation;
using Order.Data;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IValidator<CreateOrderRequest> _createOrderRequestValidator;

        public OrderService(IOrderRepository orderRepository, IValidator<CreateOrderRequest> createOrderRequestValidator)
        {
            _orderRepository = orderRepository;
            _createOrderRequestValidator = createOrderRequestValidator;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
        {
            var orders = await _orderRepository.GetOrdersAsync();
            return orders;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName)
        {
            var orders = await _orderRepository.GetOrdersByStatusAsync(statusName);
            return orders;
        }

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            return order;
        }

        public async Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, string statusName)
        {
            var result = await _orderRepository.UpdateOrderStatusAsync(orderId, statusName);
            return result;
        }

        public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request)
        {
            var validationResult = await _createOrderRequestValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return new CreateOrderResult
                {
                    Success = false,
                    Errors = validationResult.Errors.Select(x => $"{x.PropertyName} {x.ErrorMessage}").ToList()
                };
            }

            var result = await _orderRepository.CreateOrderAsync(request);
            return result;
        }
    }
}
