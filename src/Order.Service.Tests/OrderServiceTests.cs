using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;
using Order.Data;
using Order.Data.Entities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service.Tests
{
    using Model;
    using Order.Service.Validation;
    using OrderItem = Data.Entities.OrderItem;

    public class OrderServiceTests
    {
        private IOrderService _orderService;
        private IOrderRepository _orderRepository;
        private OrderContext _orderContext;
        private DbConnection _connection;

        private readonly byte[] _orderStatusCreatedId = Guid.NewGuid().ToByteArray();
        private readonly byte[] _orderStatusFailedId = Guid.NewGuid().ToByteArray();
        private readonly byte[] _orderStatusInProgressId = Guid.NewGuid().ToByteArray();
        private readonly byte[] _orderServiceEmailId = Guid.NewGuid().ToByteArray();
        private readonly byte[] _orderProductEmailId = Guid.NewGuid().ToByteArray();


        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<OrderContext>()
                .UseSqlite(CreateInMemoryDatabase())
                .EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(true)
                .Options;

            _connection = RelationalOptionsExtension.Extract(options).Connection;

            _orderContext = new OrderContext(options);
            _orderContext.Database.EnsureDeleted();
            _orderContext.Database.EnsureCreated();

            _orderRepository = new OrderRepository(_orderContext);
            _orderService = new OrderService(_orderRepository, new CreateOrderRequestValidator());

            await AddReferenceDataAsync(_orderContext);
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Dispose();
            _orderContext.Dispose();
        }


        private static DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            return connection;
        }

        [Test]
        public async Task GetOrdersAsync_ReturnsCorrectNumberOfOrders()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, 2);

            var orderId3 = Guid.NewGuid();
            await AddOrder(orderId3, 3);

            // Act
            var orders = await _orderService.GetOrdersAsync();

            // Assert
            Assert.AreEqual(3, orders.Count());
        }

        [Test]
        public async Task GetOrdersAsync_ReturnsOrdersWithCorrectTotals()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, 2);

            var orderId3 = Guid.NewGuid();
            await AddOrder(orderId3, 3);

            // Act
            var orders = await _orderService.GetOrdersAsync();

            // Assert
            var order1 = orders.SingleOrDefault(x => x.Id == orderId1);
            var order2 = orders.SingleOrDefault(x => x.Id == orderId2);
            var order3 = orders.SingleOrDefault(x => x.Id == orderId3);

            Assert.AreEqual(0.8m, order1.TotalCost);
            Assert.AreEqual(0.9m, order1.TotalPrice);

            Assert.AreEqual(1.6m, order2.TotalCost);
            Assert.AreEqual(1.8m, order2.TotalPrice);

            Assert.AreEqual(2.4m, order3.TotalCost);
            Assert.AreEqual(2.7m, order3.TotalPrice);
        }

        [Test]
        public async Task GetOrdersByStatusAsync_ReturnsOnlyOrdersWithCorrectStatus()
        {
            //Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 1);
            
            var failedOrderId = Guid.NewGuid();
            await AddOrderWithStatus(failedOrderId, 1, _orderStatusFailedId, DateTime.Now);

            //Act
            var orders = await _orderService.GetOrdersByStatusAsync("Failed");
            
            //Assert
            Assert.AreEqual(1, orders.Count());
            Assert.AreEqual(failedOrderId, orders.Single().Id);
        }

        [Test]
        public async Task GetOrdersByStatusAsync_IsCaseInsensitive()
        {
            // Arrange
            var failedOrderId = Guid.NewGuid();
            await AddOrderWithStatus(failedOrderId, 1, _orderStatusFailedId, DateTime.Now);

            // Act
            var orders = await _orderService.GetOrdersByStatusAsync("failed");

            // Assert
            Assert.AreEqual(1, orders.Count());
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsCorrectOrder()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 1);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(orderId1, order.Id);
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsCorrectOrderItemCount()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 1);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(1, order.Items.Count());
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsOrderWithCorrectTotals()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 2);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(1.6m, order.TotalCost);
            Assert.AreEqual(1.8m, order.TotalPrice);
        }
        
        [Test]
        public async Task UpdateOrderStatusAsync_UpdatesStatus_WhenOrderAndStatusExist()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            await AddOrderWithStatus(orderId, 1, _orderStatusCreatedId, DateTime.Now);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, "In Progress");

            // Assert
            Assert.AreEqual(UpdateOrderStatusOutcome.Success, result.Outcome);

            var order = await _orderService.GetOrderByIdAsync(orderId);
            Assert.AreEqual("In Progress", order.StatusName);
        }

        [Test]
        public async Task UpdateOrderStatusAsync_ReturnsSuccess_WhenStatusIsUnchanged()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            await AddOrderWithStatus(orderId, 1, _orderStatusCreatedId, DateTime.Now);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, "Created");

            // Assert
            Assert.AreEqual(UpdateOrderStatusOutcome.Success, result.Outcome);

            var order = await _orderService.GetOrderByIdAsync(orderId);
            Assert.AreEqual("Created", order.StatusName);
        }

        [Test]
        public async Task UpdateOrderStatusAsync_ReturnsOrderNotFound_WhenOrderDoesNotExist()
        {
            // Act
            var result = await _orderService.UpdateOrderStatusAsync(Guid.NewGuid(), "In Progress");

            // Assert
            Assert.AreEqual(UpdateOrderStatusOutcome.OrderNotFound, result.Outcome);
        }

        [Test]
        public async Task UpdateOrderStatusAsync_ReturnsInvalidStatus_WhenStatusDoesNotExist()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            await AddOrderWithStatus(orderId, 1, _orderStatusCreatedId, DateTime.Now);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, "NotARealStatus");

            // Assert
            Assert.AreEqual(UpdateOrderStatusOutcome.InvalidStatus, result.Outcome);
        }

        [Test]
        public async Task CreateOrderAsync_ReturnsSuccessAndCreatesOrder_WhenRequestIsValid()
        {
            // Arrange
            var request = CreateValidCreateOrderRequest();

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.OrderId.HasValue);

            var order = await _orderService.GetOrderByIdAsync(result.OrderId.Value);
            Assert.IsNotNull(order);
            Assert.AreEqual("Created", order.StatusName);
            Assert.AreEqual(1, order.Items.Count());
        }

        [Test]
        public async Task CreateOrderAsync_ReturnsValidationErrorsAndDoesNotCreateOrder_WhenRequestIsInvalid()
        {
            // Arrange
            var request = CreateValidCreateOrderRequest();
            request.ResellerId = Guid.Empty;
            request.Items[0].Quantity = 0;

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            Assert.IsFalse(result.Success);
            CollectionAssert.Contains(result.Errors, "ResellerId is required.");
            CollectionAssert.Contains(result.Errors, "Items[0].Quantity must be greater than zero.");

            var orders = await _orderService.GetOrdersAsync();
            Assert.AreEqual(0, orders.Count());
        }

        [Test]
        public async Task CreateOrderAsync_ReturnsError_WhenProductDoesNotExist()
        {
            // Arrange
            var request = CreateValidCreateOrderRequest();
            var missingProductId = Guid.NewGuid();
            request.Items[0].ProductId = missingProductId;

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            Assert.IsFalse(result.Success);
            CollectionAssert.Contains(result.Errors, $"Product {missingProductId} not found");

            var orders = await _orderService.GetOrdersAsync();
            Assert.AreEqual(0, orders.Count());
        }

        private CreateOrderRequest CreateValidCreateOrderRequest()
        {
            return new CreateOrderRequest
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemRequest>
                {
                    new CreateOrderItemRequest
                    {
                        ProductId = new Guid(_orderProductEmailId),
                        Quantity = 1
                    }
                }
            };
        }

        private async Task AddOrder(Guid orderId, int quantity)
        {
            var orderIdBytes = orderId.ToByteArray();
            _orderContext.Order.Add(new Data.Entities.Order
            {
                Id = orderIdBytes,
                ResellerId = Guid.NewGuid().ToByteArray(),
                CustomerId = Guid.NewGuid().ToByteArray(),
                CreatedDate = DateTime.Now,
                StatusId = _orderStatusCreatedId,
            });

            _orderContext.OrderItem.Add(new OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderIdBytes,
                ServiceId = _orderServiceEmailId,
                ProductId = _orderProductEmailId,
                Quantity = quantity
            });

            await _orderContext.SaveChangesAsync();
        }
        
        private async Task AddOrderWithStatus(Guid orderId, int quantity, byte[] statusId, DateTime createdDate)
        {
            var orderIdBytes = orderId.ToByteArray();
            _orderContext.Order.Add(new Data.Entities.Order
            {
                Id = orderIdBytes,
                ResellerId = Guid.NewGuid().ToByteArray(),
                CustomerId = Guid.NewGuid().ToByteArray(),
                CreatedDate = createdDate,
                StatusId = statusId,
            });

            _orderContext.OrderItem.Add(new OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderIdBytes,
                ServiceId = _orderServiceEmailId,
                ProductId = _orderProductEmailId,
                Quantity = quantity
            });

            await _orderContext.SaveChangesAsync();
        }

        private async Task AddReferenceDataAsync(OrderContext orderContext)
        {
            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = _orderStatusCreatedId,
                Name = "Created",
            });

            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = _orderStatusFailedId,
                Name = "Failed",
            });

            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = _orderStatusInProgressId,
                Name = "In Progress",
            });

            orderContext.OrderService.Add(new Data.Entities.OrderService
            {
                Id = _orderServiceEmailId,
                Name = "Email"
            });

            orderContext.OrderProduct.Add(new OrderProduct
            {
                Id = _orderProductEmailId,
                Name = "100GB Mailbox",
                UnitCost = 0.8m,
                UnitPrice = 0.9m,
                ServiceId = _orderServiceEmailId
            });

            await orderContext.SaveChangesAsync();
        }
    }
}
