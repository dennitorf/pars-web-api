using Azure.Identity;
using Azure.Storage.Blobs;
using KellyServices.PARS.Application.Common.Interfaces.Archive;
using KellyServices.PARS.Application.Common.Interfaces.Email;
using KellyServices.PARS.Infrastructure.Archive;
using KellyServices.PARS.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KellyServices.PARS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IEmailService, EmailService>();
            services.Configure<AzureBlobOptions>(configuration.GetSection(AzureBlobOptions.SectionName));
            services.Configure<SftpOptions>(configuration.GetSection(SftpOptions.SectionName));
            services.AddSingleton(serviceProvider =>
            {
                var options = configuration.GetSection(AzureBlobOptions.SectionName).Get<AzureBlobOptions>();
                if (!string.IsNullOrWhiteSpace(options.ConnectionString)) return new BlobServiceClient(options.ConnectionString);
                if (string.IsNullOrWhiteSpace(options.ServiceUri)) throw new System.InvalidOperationException("ArchiveStorage ServiceUri is required when ConnectionString is not configured.");
                return new BlobServiceClient(new System.Uri(options.ServiceUri), new DefaultAzureCredential());
            });
            services.AddSingleton<IArchiveFileStore, AzureBlobArchiveFileStore>();
            services.AddSingleton<ISftpArchiveSource, SftpArchiveSource>();

            return services;
        }
    }
}
