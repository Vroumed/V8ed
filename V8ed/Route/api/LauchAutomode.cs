using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Models;
using Vroumed.V8ed.Models.HttpModels;
using Vroumed.V8ed.Models.Users;

namespace Vroumed.V8ed.Route.api;

[Route("automode")]
[ApiController]
public class AutomodeAPI : ControllerBase
{
  private readonly DependencyInjector _injector;
  private readonly HttpContext _context;
  public AutomodeAPI(IHttpContextAccessor context, DependencyInjector injector)
  {
    _injector = injector;
    _context = context.HttpContext!;
  }

  [HttpPost]
  public async Task<IActionResult> ActivateAutomode([FromBody] bool auto)
  {
    UserSession session = UserSession.FromContext(_context);
    if (!session.Logged)
      return Unauthorized(this.GetStatusError(HttpStatusCode.Unauthorized, "auth", "You are not connected"));

    if (auto && session.AutoEngine != null)
      return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "auth", "You are already in automode"));

    if (!auto && session.AutoEngine == null)
      return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "auth", "You are not in automode"));

    if (!auto)
    {
      session.RoverManager.OnRoverReading -= session.AutoEngine!.Perform;
      session.AutoEngine = null;
    }
    else
    {
      session.AutoEngine = new Managers.Engines.RoverAutoEngine() 
      { 
        RoverManager = session.RoverManager
      };

      session.RoverManager.OnRoverReading += session.AutoEngine!.Perform;
    }
  }
}