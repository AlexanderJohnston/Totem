using Microsoft.AspNetCore.Mvc;
using Quantum.Web.IdentityTracking;

namespace Quantum.Web.Controllers
{
  [ApiController]
  [Route("api/session")]
  public sealed class SessionController : ControllerBase
  {
    readonly IInteractionIdentityResolver _identity;

    public SessionController(IInteractionIdentityResolver identity)
    {
      _identity = identity;
    }

    [HttpGet]
    public ActionResult<InteractionSession> Get()
    {
      Response.Headers["Cache-Control"] = "no-store";

      return Ok(_identity.Resolve(HttpContext.User));
    }
  }
}
