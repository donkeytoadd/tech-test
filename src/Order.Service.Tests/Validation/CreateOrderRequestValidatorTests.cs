using FluentValidation.TestHelper;
using NUnit.Framework;
using Order.Model;
using System;
using System.Collections.Generic;

namespace Order.Service.Tests.Validation
{
    using Service.Validation;

    public class CreateOrderRequestValidatorTests
    {
        private CreateOrderRequestValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new CreateOrderRequestValidator();
        }

        [Test]
        public void HasError_WhenResellerIdIsEmpty()
        {
            var request = CreateValidRequest();
            request.ResellerId = Guid.Empty;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ResellerId)
                .WithErrorMessage("is required.");
        }

        [Test]
        public void HasError_WhenCustomerIdIsEmpty()
        {
            var request = CreateValidRequest();
            request.CustomerId = Guid.Empty;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.CustomerId)
                .WithErrorMessage("is required.");
        }

        [Test]
        public void HasError_WhenItemsIsEmpty()
        {
            var request = CreateValidRequest();
            request.Items = new List<CreateOrderItemRequest>();

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Items)
                .WithErrorMessage("must contain at least one item.");
        }

        [Test]
        public void HasError_WhenAnItemHasAnEmptyProductId()
        {
            var request = CreateValidRequest();
            request.Items[0].ProductId = Guid.Empty;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor("Items[0].ProductId");
        }

        [Test]
        public void HasError_WhenAnItemHasAZeroQuantity()
        {
            var request = CreateValidRequest();
            request.Items[0].Quantity = 0;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor("Items[0].Quantity");
        }

        [Test]
        public void HasNoErrors_WhenRequestIsValid()
        {
            var request = CreateValidRequest();

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }

        private static CreateOrderRequest CreateValidRequest()
        {
            return new CreateOrderRequest
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemRequest>
                {
                    new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }
                }
            };
        }
    }
}
