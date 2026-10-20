using FluentValidation;

namespace Pymen.Application.Production;

public sealed class CreateWorkOrderRequestValidator : AbstractValidator<CreateWorkOrderRequest>
{
    public CreateWorkOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
        RuleForEach(x => x.Operations).ChildRules(op =>
        {
            op.RuleFor(o => o.OperationTypeId).NotEmpty();
        }).When(x => x.Operations is not null);
    }
}

public sealed class UpdateWorkOrderRequestValidator : AbstractValidator<UpdateWorkOrderRequest>
{
    public UpdateWorkOrderRequestValidator()
    {
        RuleForEach(x => x.LinesToAdd).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        }).When(x => x.LinesToAdd is not null);

        RuleForEach(x => x.OperationsToAdd).ChildRules(op =>
        {
            op.RuleFor(o => o.OperationTypeId).NotEmpty();
        }).When(x => x.OperationsToAdd is not null);
    }
}

public sealed class CreateDeliveryNoteRequestValidator : AbstractValidator<CreateDeliveryNoteRequest>
{
    public CreateDeliveryNoteRequestValidator()
    {
        RuleFor(x => x.WorkOrderId).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
