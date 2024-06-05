using Microsoft.AspNetCore.Mvc;
using System.Net;
using V8ed.Extensions;
using V8ed.Managers;
using V8ed.Models.Users;

namespace V8ed.Controllers;

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
    {
      return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "auth", "You are already logged in"));
    }

    //TODO Check if rover exists
    bool roverFound = true; //ignore for now
    if (!roverFound)
    {
      return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "auth", "Invalid login or password"));
    }

    session.LinkedRover = login.AuthenthicationKey;
    
    

    Dictionary<string, string> data = new Dictionary<string, string>();
    data.Add("token", session.SessionId);
    return Ok(data);

  }
}

