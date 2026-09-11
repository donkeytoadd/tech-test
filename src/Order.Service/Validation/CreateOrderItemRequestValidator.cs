namespace Order.Service.Validation
{
    using FluentValidation;
    using Order.Model;

    public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
    {
        public CreateOrderItemRequestValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("is required.");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("must be greater than zero.");
        }
    }
}
