using KellyServices.PARS.Application.Features.PayrollRequests.Models;
using MediatR;
using System;
namespace KellyServices.PARS.Application.Features.PayrollRequests.Commands.SubmitPayrollRequest
{ public class SubmitPayrollRequestCommand : IRequest<PayrollRequestDetail> { public string FirstName { get; set; } public string LastName { get; set; } public string Email { get; set; } public string KellyId { get; set; } public string TaxIdLastFour { get; set; } public DateTime FromDate { get; set; } public DateTime ToDate { get; set; } public string DocumentTypes { get; set; } } }
