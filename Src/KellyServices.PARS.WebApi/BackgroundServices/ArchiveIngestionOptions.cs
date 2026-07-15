namespace KellyServices.PARS.WebApi.BackgroundServices
{
    public class ArchiveIngestionOptions
    {
        public const string SectionName = "ArchiveIngestion";
        public bool Enabled { get; set; }
        public int PollIntervalSeconds { get; set; } = 300;
        public int MaxRecordsPerRun { get; set; } = 5000;
        public string MetadataRemotePath { get; set; } = "/outbound/pars/archive-metadata.csv";
        public string ProcessedDirectory { get; set; } = "/outbound/pars/processed";
        public string BlobContainer { get; set; } = "payroll-archive";
    }
}
