using KellyServices.PARS.Application.Features.PayrollRequests.Models; using MediatR; using System.Collections.Generic;
namespace KellyServices.PARS.Application.Features.PayrollRequests.Queries.GetPayrollRequests
{ public class GetPayrollRequestsQuery : IRequest<IReadOnlyList<PayrollRequestSummary>> { public string Status { get; set; } public string Search { get; set; } } }
