using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KellyServices.PARS.Application.Common.Interfaces.Archive
{
    public interface ISftpArchiveSource
    {
        Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken);
        Task DownloadAsync(string remotePath, Stream destination, CancellationToken cancellationToken);
        Task MoveToProcessedAsync(string remotePath, string processedDirectory, CancellationToken cancellationToken);
    }
}
