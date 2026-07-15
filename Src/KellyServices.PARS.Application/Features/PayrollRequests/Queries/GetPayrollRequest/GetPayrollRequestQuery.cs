using KellyServices.PARS.Application.Features.PayrollRequests.Models; using MediatR; using System;
namespace KellyServices.PARS.Application.Features.PayrollRequests.Queries.GetPayrollRequest
{ public record GetPayrollRequestQuery(Guid RequestId) : IRequest<PayrollRequestDetail>; }
