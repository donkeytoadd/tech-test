using FluentValidation.TestHelper;
using NUnit.Framework;
using Order.Model;
using System;

namespace Order.Service.Tests.Validation
{
    using Service.Validation;

    public class CreateOrderItemRequestValidatorTests
    {
        private CreateOrderItemRequestValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new CreateOrderItemRequestValidator();
        }

        [Test]
        public void HasError_WhenProductIdIsEmpty()
        {
            var request = new CreateOrderItemRequest { ProductId = Guid.Empty, Quantity = 1 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ProductId)
                .WithErrorMessage("is required.");
        }

        [Test]
        public void HasError_WhenQuantityIsZero()
        {
            var request = new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 0 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Quantity)
                .WithErrorMessage("must be greater than zero.");
        }

        [Test]
        public void HasError_WhenQuantityIsNegative()
        {
            var request = new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = -1 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Quantity);
        }

        [Test]
        public void HasNoErrors_WhenRequestIsValid()
        {
            var request = new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 };

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
