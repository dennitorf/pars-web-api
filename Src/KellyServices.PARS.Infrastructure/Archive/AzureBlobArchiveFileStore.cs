using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using KellyServices.PARS.Application.Common.Interfaces.Archive;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KellyServices.PARS.Infrastructure.Archive
{
    public class AzureBlobArchiveFileStore : IArchiveFileStore
    {
        private readonly BlobServiceClient blobServiceClient;

        public AzureBlobArchiveFileStore(BlobServiceClient blobServiceClient)
        {
            this.blobServiceClient = blobServiceClient;
        }

        public async Task UploadAsync(string containerName, string blobName, Stream content, string contentType, IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken)
        {
            var container = blobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            var blob = container.GetBlobClient(blobName);
            content.Position = 0;
            try
            {
                await blob.UploadAsync(content, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Metadata = new Dictionary<string, string>(metadata),
                    Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
                }, cancellationToken);
            }
            catch (RequestFailedException exception) when (exception.Status == 409 || exception.Status == 412)
            {
                // Blob names are checksum-derived. An existing blob means a prior attempt already uploaded this exact payload.
            }
        }

        public async Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            var blob = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
            return await blob.OpenReadAsync(new BlobOpenReadOptions(false), cancellationToken);
        }
    }
}
