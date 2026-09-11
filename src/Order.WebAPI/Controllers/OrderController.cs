using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order.Service;
using System;
using System.Threading.Tasks;

namespace OrderService.WebAPI.Controllers
{
    using Order.Model;

    [ApiController]
    [Route("orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("status/{statusName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByStatusAsync(string statusName)
        {
            var orders = await _orderService.GetOrdersByStatusAsync(statusName);
            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                return Ok(order);
            }
            else
            {
                return NotFound();
            }
        }
        
        [HttpPut("{orderId}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            if (request == null)
            {
                return BadRequest("A request body is required.");
            }

            var result = await _orderService.UpdateOrderStatusAsync(orderId, request.Status);

            switch (result.Outcome)
            {
                case UpdateOrderStatusOutcome.Success:
                    return NoContent();
                case UpdateOrderStatusOutcome.OrderNotFound:
                    return NotFound($"Order '{orderId}' was not found.");
                case UpdateOrderStatusOutcome.InvalidStatus:
                    return BadRequest($"Order status '{request.Status}' is not valid.");
                default:
                    return BadRequest();
            }
        }
    }
}
