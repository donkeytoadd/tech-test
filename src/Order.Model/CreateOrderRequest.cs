namespace Order.Model
{
    using System;
    using System.Collections.Generic;

    public class CreateOrderRequest
    {
        public Guid ResellerId { get; set; }
        public Guid CustomerId { get; set; }
        public List<CreateOrderItemRequest> Items { get; set; } = new List<CreateOrderItemRequest>();
    }
}
