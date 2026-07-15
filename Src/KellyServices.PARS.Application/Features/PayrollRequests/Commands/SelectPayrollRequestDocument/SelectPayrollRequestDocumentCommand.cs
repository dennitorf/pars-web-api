using KellyServices.PARS.Application.Features.PayrollRequests.Models; using MediatR; using System;
namespace KellyServices.PARS.Application.Features.PayrollRequests.Commands.SelectPayrollRequestDocument
{ public class SelectPayrollRequestDocumentCommand : IRequest<PayrollRequestDetail> { public Guid RequestId { get; set; } public Guid DocumentId { get; set; } public bool IsSelected { get; set; } } }
