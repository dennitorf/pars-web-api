using KellyServices.PARS.Application.Common.Interfaces.Email;
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

            return services;
        }
    }
}
