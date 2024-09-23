using System.Net;
using Vroumed.V8ed.Managers;
using Vroumed.V8ed.Managers.Engines;

namespace Vroumed.V8ed.Models.Users;

public class UserSession
{
  public string SessionId { get; }
  public bool Logged => RoverManager.Connected;
  public IPAddress? ClientIP { get; init; }
  public IPAddress? RoverIP { get; set; }
  public DateTime LastActivity { get; set; } = DateTime.UtcNow;
  public RoverManager RoverManager { get; } = new();

  public UserSession(string sessionId, IPAddress? clientIp)
  {
    SessionId = sessionId;
    ClientIP = clientIp;
  }

  public static UserSession FromContext(HttpContext context)
  {
    return (context.Items["session"] as UserSession)!;
  }
}