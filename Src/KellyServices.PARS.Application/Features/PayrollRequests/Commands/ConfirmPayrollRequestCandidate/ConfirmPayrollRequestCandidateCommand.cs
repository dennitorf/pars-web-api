using KellyServices.PARS.Application.Features.PayrollRequests.Models;
using MediatR;
using System;
namespace KellyServices.PARS.Application.Features.PayrollRequests.Commands.ConfirmPayrollRequestCandidate
{ public class ConfirmPayrollRequestCandidateCommand : IRequest<PayrollRequestDetail> { public Guid RequestId { get; set; } public Guid CandidateId { get; set; } public string Notes { get; set; } } }
