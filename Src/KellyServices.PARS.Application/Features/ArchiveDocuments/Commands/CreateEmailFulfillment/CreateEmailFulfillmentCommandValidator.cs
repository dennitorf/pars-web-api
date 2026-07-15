using FluentValidation;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.CreateEmailFulfillment
{
    public class CreateEmailFulfillmentCommandValidator : AbstractValidator<CreateEmailFulfillmentCommand>
    {
        public CreateEmailFulfillmentCommandValidator() { RuleFor(item => item.DocumentId).NotEmpty(); RuleFor(item => item.EmployeeEmail).NotEmpty().EmailAddress().MaximumLength(320); RuleFor(item => item.BusinessReason).NotEmpty().MaximumLength(1000); }
    }
}
