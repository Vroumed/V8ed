using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using Vroumed.V8ed.Models;

namespace Vroumed.V8ed.Managers;

public class RoverManager
{

  public enum ConnectionStatus
  {
    Ok,
    Busy, 
    NoClient,
    WrongApiKey,
  }

  struct ConnectionString
  {
    public ConnectionString()
    {
    }

    [JsonProperty("type")] 
    public byte Type { get; } = 1;

    [JsonProperty("apiKey")]
    public required string ApiKey { get; init; }
  }


  public int ConnectionAttempt { get; set; } = 0;

  #region Telemetry

  public Car Car { get; set; } = null!;
  public Connection Connection { get; set; } = null!;


  #endregion


  private ClientWebSocket WebSocket { get; } = new ClientWebSocket();
  private Task WebscoketEventLoop { get; set; }
  private CancellationTokenSource CancellationTokenSource { get; set; }

  public bool Connected { get; private set; }

  public async Task<ConnectionStatus> Connect(Uri url, string apiKey)
  {
    await WebSocket.ConnectAsync(url, CancellationToken.None);

    ConnectionString connection = new()
    {
      ApiKey = apiKey
    };

    string message = JsonConvert.SerializeObject(connection);
    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
    ArraySegment<byte> sendBuffer = new(messageBytes);
    await WebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);

    ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[1024]);
    WebSocketReceiveResult result = await WebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
    string receivedMessage = Encoding.UTF8.GetString(receiveBuffer.Array, 0, result.Count);

    if (receivedMessage == "ok")
    {
      Connected = true;
      CancellationTokenSource = new CancellationTokenSource();
      await StartEventLoop(CancellationTokenSource.Token);
      return ConnectionStatus.Ok;
    }

    return receivedMessage.Split(':').Last().ToLower() switch
    {
      "client" => ConnectionStatus.NoClient,
      "occupied" => ConnectionStatus.Busy,
      "unauthorized" => ConnectionStatus.WrongApiKey,
      _ => throw new Exception($"Unknown rover message : {receivedMessage}")
    };
  }

  private Task StartEventLoop(CancellationToken token)
  {
    return WebscoketEventLoop = Task.Run(EventLoop, token);
  }

  private async void EventLoop()
  {
    while (Connected)
    {
      ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[1024]);
      WebSocketReceiveResult result = await WebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
      string receivedMessage = Encoding.UTF8.GetString(receiveBuffer.Array, 0, result.Count);
      await HandlePacket(receivedMessage);
    }
  }

  private async Task HandlePacket(string receivedMessage)
  {
    
  }
}
