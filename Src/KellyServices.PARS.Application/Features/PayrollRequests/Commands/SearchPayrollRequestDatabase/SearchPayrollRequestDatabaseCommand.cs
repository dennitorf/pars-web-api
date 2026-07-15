using KellyServices.PARS.Application.Features.PayrollRequests.Models; using MediatR; using System;
namespace KellyServices.PARS.Application.Features.PayrollRequests.Commands.SearchPayrollRequestDatabase
{ public record SearchPayrollRequestDatabaseCommand(Guid RequestId) : IRequest<PayrollRequestDetail>; }
