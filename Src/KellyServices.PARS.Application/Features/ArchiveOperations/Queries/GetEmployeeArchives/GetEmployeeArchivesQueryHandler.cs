using KellyServices.PARS.Application.Features.ArchiveOperations.Models;
using KellyServices.PARS.Domain.Enums;
using KellyServices.PARS.Persistence.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetEmployeeArchives
{
    public class GetEmployeeArchivesQueryHandler : IRequestHandler<GetEmployeeArchivesQuery, EmployeeArchiveSearchResponse>
    {
        private readonly AppDbContext db;
        public GetEmployeeArchivesQueryHandler(AppDbContext db) => this.db = db;
        public async Task<EmployeeArchiveSearchResponse> Handle(GetEmployeeArchivesQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(1, request.Page); var pageSize = Math.Clamp(request.PageSize, 1, 200);
            var query = db.EmployeeArchives.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Query)) query = query.Where(item => item.KellyId.Contains(request.Query) || item.EmployeeName.Contains(request.Query));
            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ArchiveStorageStatus>(request.Status, true, out var status)) query = query.Where(item => item.StorageStatus == status);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderBy(item => item.EmployeeName).Skip((page - 1) * pageSize).Take(pageSize).Select(item => new EmployeeArchiveSummary(item.Id, item.KellyId, item.EmployeeName,
                item.Documents.Count(document => document.Status == ArchiveDocumentStatus.Available), item.Documents.Count(document => document.Status == ArchiveDocumentStatus.Available && document.DocumentType == "W-2"),
                item.Documents.Count(document => document.Status == ArchiveDocumentStatus.Available && document.DocumentType == "Paystub"), item.Documents.Where(document => document.Status == ArchiveDocumentStatus.Available).Min(document => (int?)document.DocumentYear),
                item.Documents.Where(document => document.Status == ArchiveDocumentStatus.Available).Max(document => (int?)document.DocumentYear), item.StorageStatus.ToString(), item.ModifiedDate == default ? item.CreatedDate : item.ModifiedDate)).ToListAsync(cancellationToken);
            return new EmployeeArchiveSearchResponse(total, page, pageSize, items);
        }
    }
}
