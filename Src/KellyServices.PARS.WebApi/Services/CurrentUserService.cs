using KellyServices.PARS.Application.Common.Interfaces.Identity;
using Microsoft.AspNetCore.Http;

namespace KellyServices.PARS.WebApi.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        public CurrentUserService(IHttpContextAccessor httpContextAccessor) => this.httpContextAccessor = httpContextAccessor;
        public string UserId => httpContextAccessor.HttpContext?.User?.FindFirst("oid")?.Value
            ?? httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
        public string DisplayName => httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "PARS user";
        public string CorrelationId => httpContextAccessor.HttpContext?.TraceIdentifier ?? System.Guid.NewGuid().ToString();
    }
}
