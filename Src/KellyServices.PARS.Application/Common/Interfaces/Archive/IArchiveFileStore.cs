using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KellyServices.PARS.Application.Common.Interfaces.Archive
{
    public interface IArchiveFileStore
    {
        Task UploadAsync(string containerName, string blobName, Stream content, string contentType, IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken);
        Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken);
    }
}
