namespace KellyServices.PARS.Infrastructure.Archive
{
    public class AzureBlobOptions
    {
        public const string SectionName = "ArchiveStorage";
        public string ServiceUri { get; set; }
        public string ConnectionString { get; set; }
        public string ContainerName { get; set; } = "payroll-archive";
    }
}
