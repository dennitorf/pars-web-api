using KellyServices.PARS.Application.Features.ArchiveDocuments.Models;
using KellyServices.PARS.Domain.Entities.Archive;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments
{
    internal static class ArchiveDocumentMapping
    {
        internal static ArchiveDocumentSummary ToSummary(this ArchiveDocument item) => new(item.Id, item.EmployeeArchive.KellyId, item.EmployeeArchive.EmployeeName,
            item.EmployeeArchive.MaskedTaxId, item.DocumentType, item.DocumentYear, item.DocumentPeriod, item.FileSizeBytes, item.Status.ToString());
    }
}
