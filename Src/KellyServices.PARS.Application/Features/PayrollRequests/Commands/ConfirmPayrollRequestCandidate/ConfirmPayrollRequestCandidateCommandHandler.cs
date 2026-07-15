using KellyServices.PARS.Application.Features.PayrollRequests.Models;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.Application.Features.PayrollRequests.Commands.ConfirmPayrollRequestCandidate
{ public class ConfirmPayrollRequestCandidateCommandHandler : IRequestHandler<ConfirmPayrollRequestCandidateCommand, PayrollRequestDetail> { private readonly PayrollRequestWorkflowService service; public ConfirmPayrollRequestCandidateCommandHandler(PayrollRequestWorkflowService service) => this.service = service; public Task<PayrollRequestDetail> Handle(ConfirmPayrollRequestCandidateCommand r, CancellationToken c) => service.ConfirmAsync(r.RequestId, r.CandidateId, r.Notes, c); } }
