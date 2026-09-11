namespace Order.Model
{
    using System;
    using System.Collections.Generic;

    public class CreateOrderResult
    {
        public bool Success { get; set; }
        public Guid? OrderId { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}