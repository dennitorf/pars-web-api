namespace KellyServices.PARS.Application.Features.ArchiveIngestion
{
    public record ArchiveImportRecord(
        string KellyId,
        string EmployeeName,
        string MaskedTaxId,
        string DocumentType,
        int DocumentYear,
        string DocumentPeriod,
        string RemoteFilePath,
        long FileSizeBytes,
        string Sha256);
}
