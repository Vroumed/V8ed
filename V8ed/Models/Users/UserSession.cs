using System.Net;
using System.Net.WebSockets;

namespace V8ed.Models.Users;

public class UserSession
{
  public string? LinkedRover { get; set; }
  public string SessionId { get; }
  public bool Logged => LinkedRover != null;
  public IPAddress? IpAddress { get; init; }
  public DateTime LastActivity { get; set; } = DateTime.UtcNow;

  public UserSession(string sessionId, IPAddress? ipAddress)
  {
    SessionId = sessionId;
    IpAddress = ipAddress;
  }

  public static UserSession FromContext(HttpContext context)
  {
    return (context.Items["session"] as UserSession)!;
  }
}