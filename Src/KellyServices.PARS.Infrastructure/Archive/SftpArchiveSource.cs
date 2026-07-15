using KellyServices.PARS.Application.Common.Interfaces.Archive;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace KellyServices.PARS.Infrastructure.Archive
{
    public class SftpArchiveSource : ISftpArchiveSource
    {
        private readonly SftpOptions options;

        public SftpArchiveSource(IOptions<SftpOptions> options)
        {
            this.options = options.Value;
        }

        public Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken) => Run(client => client.Exists(remotePath), cancellationToken);

        public Task DownloadAsync(string remotePath, Stream destination, CancellationToken cancellationToken) => Run(client =>
        {
            client.DownloadFile(remotePath, destination);
            destination.Position = 0;
            return true;
        }, cancellationToken);

        public Task MoveToProcessedAsync(string remotePath, string processedDirectory, CancellationToken cancellationToken) => Run(client =>
        {
            EnsureDirectory(client, processedDirectory);
            var fileName = remotePath.Split('/').Last();
            var destination = $"{processedDirectory.TrimEnd('/')}/{fileName}";
            if (client.Exists(destination)) destination = $"{processedDirectory.TrimEnd('/')}/{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{fileName}";
            client.RenameFile(remotePath, destination);
            return true;
        }, cancellationToken);

        private async Task<T> Run<T>(Func<SftpClient, T> operation, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var client = CreateClient();
                client.Connect();
                cancellationToken.ThrowIfCancellationRequested();
                try { return operation(client); }
                finally { if (client.IsConnected) client.Disconnect(); }
            }, cancellationToken);
        }

        private SftpClient CreateClient()
        {
            if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.Username))
                throw new InvalidOperationException("ArchiveSftp Host and Username are required.");
            if (string.IsNullOrWhiteSpace(options.ExpectedHostKeySha256))
                throw new InvalidOperationException("ArchiveSftp ExpectedHostKeySha256 is required to prevent host impersonation.");

            AuthenticationMethod authentication;
            if (!string.IsNullOrWhiteSpace(options.PrivateKeyBase64))
            {
                var keyBytes = Convert.FromBase64String(options.PrivateKeyBase64);
                var keyStream = new MemoryStream(keyBytes, writable: false);
                var key = string.IsNullOrWhiteSpace(options.PrivateKeyPassphrase) ? new PrivateKeyFile(keyStream) : new PrivateKeyFile(keyStream, options.PrivateKeyPassphrase);
                authentication = new PrivateKeyAuthenticationMethod(options.Username, key);
            }
            else if (!string.IsNullOrWhiteSpace(options.Password))
            {
                authentication = new PasswordAuthenticationMethod(options.Username, options.Password);
            }
            else throw new InvalidOperationException("ArchiveSftp requires Password or PrivateKeyBase64 authentication.");

            var connection = new ConnectionInfo(options.Host, options.Port, options.Username, authentication)
            {
                Timeout = TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds)
            };
            var client = new SftpClient(connection);
            client.HostKeyReceived += ValidateHostKey;
            return client;
        }

        private void ValidateHostKey(object sender, HostKeyEventArgs args)
        {
            var actual = $"SHA256:{Convert.ToBase64String(SHA256.HashData(args.HostKey)).TrimEnd('=')}";
            var expected = options.ExpectedHostKeySha256.Trim().TrimEnd('=');
            args.CanTrust = actual.Equals(expected, StringComparison.Ordinal);
        }

        private static void EnsureDirectory(SftpClient client, string path)
        {
            var current = path.StartsWith('/') ? "/" : string.Empty;
            foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                current = current == "/" ? $"/{segment}" : string.IsNullOrEmpty(current) ? segment : $"{current}/{segment}";
                if (!client.Exists(current)) client.CreateDirectory(current);
            }
        }
    }
}
