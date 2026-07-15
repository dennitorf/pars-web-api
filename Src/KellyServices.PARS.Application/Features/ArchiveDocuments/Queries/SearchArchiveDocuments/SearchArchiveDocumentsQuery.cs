using KellyServices.PARS.Application.Features.ArchiveDocuments.Models;
using MediatR;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Queries.SearchArchiveDocuments
{
    public class SearchArchiveDocumentsQuery : IRequest<SearchArchiveResponse>
    {
        public string Employee { get; set; } public string DocumentType { get; set; } public int? FromYear { get; set; } public int? ToYear { get; set; }
        public int Page { get; set; } = 1; public int PageSize { get; set; } = 50;
    }
}
