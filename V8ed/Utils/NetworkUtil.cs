using System.Net;
using System.Net.Sockets;

namespace Vroumed.V8ed.Utils;

public static class NetworkUtil
{
  public static string? GetLocalIPAddress()
  {
    return (from ip
          in Dns.GetHostEntry(Dns.GetHostName()).AddressList
            where ip.AddressFamily == AddressFamily.InterNetwork
            select ip.ToString())
      .FirstOrDefault();
  }
}