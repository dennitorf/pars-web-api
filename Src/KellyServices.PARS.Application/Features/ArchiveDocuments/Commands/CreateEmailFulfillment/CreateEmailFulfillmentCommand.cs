using KellyServices.PARS.Application.Features.ArchiveDocuments.Models;
using MediatR;
using System;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.CreateEmailFulfillment
{
    public class CreateEmailFulfillmentCommand : IRequest<EmailFulfillmentResponse>
    { public Guid DocumentId { get; set; } public string EmployeeEmail { get; set; } public string BusinessReason { get; set; } }
}
