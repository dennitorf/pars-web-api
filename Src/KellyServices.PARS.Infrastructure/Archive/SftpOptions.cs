namespace KellyServices.PARS.Infrastructure.Archive
{
    public class SftpOptions
    {
        public const string SectionName = "ArchiveSftp";
        public string Host { get; set; }
        public int Port { get; set; } = 22;
        public string Username { get; set; }
        public string Password { get; set; }
        public string PrivateKeyBase64 { get; set; }
        public string PrivateKeyPassphrase { get; set; }
        public string ExpectedHostKeySha256 { get; set; }
        public int ConnectionTimeoutSeconds { get; set; } = 30;
    }
}
