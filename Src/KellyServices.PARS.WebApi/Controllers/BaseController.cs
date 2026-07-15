using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KellyServices.PARS.WebApi.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected BaseController(IMediator mediator) => Mediator = mediator;
        protected IMediator Mediator { get; }
    }
}
