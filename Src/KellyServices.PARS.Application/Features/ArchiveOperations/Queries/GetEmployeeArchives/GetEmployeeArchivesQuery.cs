using KellyServices.PARS.Application.Features.ArchiveOperations.Models;
using MediatR;
namespace KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetEmployeeArchives
{
    public class GetEmployeeArchivesQuery : IRequest<EmployeeArchiveSearchResponse>
    {
        public string Query { get; set; }
        public string Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
