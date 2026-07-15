using KellyServices.PARS.Application.Features.PayrollRequests.Models; using MediatR; using System;
namespace KellyServices.PARS.Application.Features.PayrollRequests.Commands.FulfillPayrollRequest
{ public class FulfillPayrollRequestCommand : IRequest<PayrollRequestDetail> { public Guid RequestId { get; set; } public string EmployeeEmail { get; set; } public string SpecialistNotes { get; set; } } }
