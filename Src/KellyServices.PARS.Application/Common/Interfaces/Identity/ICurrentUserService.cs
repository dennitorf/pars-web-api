namespace KellyServices.PARS.Application.Common.Interfaces.Identity
{
    public interface ICurrentUserService
    {
        string UserId { get; }
        string DisplayName { get; }
        string CorrelationId { get; }
    }
}
