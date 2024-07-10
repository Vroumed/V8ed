using Microsoft.AspNetCore.Mvc;
using System.Net;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Models.Users;

namespace Vroumed.V8ed.Controllers;

[ApiController]
[Route("auth")]
public class Auth : ControllerBase
{
  private readonly HttpContext _context;
  public Auth(IHttpContextAccessor context)
  {
    _context = context.HttpContext!;
  }

  public class AuthModel
  {
    public string AuthenthicationKey { get; set; }
  }

  [HttpPost]
  public async Task<IActionResult> Post([FromBody] AuthModel login)
  {

    UserSession session = UserSession.FromContext(_context);
    if (session.Logged)
      return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "auth", "You are already logged in"));

    //TODO Check if rover exists
    bool roverFound = true; //ignore for now
    if (!roverFound)
      return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "auth", "Invalid login or password"));

    session.LinkedRover = login.AuthenthicationKey;

    Dictionary<string, string> data = new()
    {
      { "token", session.SessionId }
    };
    return Ok(data);

  }
}