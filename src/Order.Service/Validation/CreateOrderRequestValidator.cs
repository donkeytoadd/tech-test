namespace Order.Service.Validation
{
    using FluentValidation;
    using Order.Model;

    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.ResellerId).NotEmpty().WithMessage("is required.");
            RuleFor(x => x.CustomerId).NotEmpty().WithMessage("is required.");
            RuleFor(x => x.Items).NotEmpty().WithMessage("must contain at least one item.");

            RuleForEach(x => x.Items).SetValidator(new CreateOrderItemRequestValidator());
        }
    }
}
