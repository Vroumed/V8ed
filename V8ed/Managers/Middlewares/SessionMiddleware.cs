using V8ed.Models.Users;

namespace V8ed.Managers.Middlewares;

public class SessionMiddleware
{
  
  private readonly RequestDelegate _next;
  private readonly SessionManager _manager;

  public SessionMiddleware(RequestDelegate next, SessionManager manager)
  {
    _next = next;
    _manager = manager;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    UserSession session = _manager.IdentifyUser(context, true);
    context.Items.Add("session", session);
    await _next(context);
  }
}
