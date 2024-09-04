using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using Vroumed.V8ed.Models;
using Vroumed.V8ed.Models.Rover;

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

  readonly struct ConnectionString
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

  public List<(DateTime time, RoverReading reading)> Readings { get; set; }

  #endregion

  public ClientWebSocket WebSocket { get; } = new();
  private Task WebsocketEventLoop { get; set; } = null!;
  private CancellationTokenSource CancellationTokenSource { get; set; } = null!;

  public bool Connected { get; private set; }

  /// <summary>
  /// event trigger that can be sub to receive rover readings
  /// </summary>
  public event Action<RoverReading> OnRoverReading;

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

    ArraySegment<byte> receiveBuffer = new(new byte[1024]);
    WebSocketReceiveResult result = await WebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
    string receivedMessage = Encoding.UTF8.GetString(receiveBuffer.Array!, 0, result.Count);

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
    return WebsocketEventLoop = Task.Run(EventLoop, token);
  }

  private async void EventLoop()
  {
    try
    {
      while (Connected)
      {
        ArraySegment<byte> receiveBuffer = new(new byte[1024]);
        WebSocketReceiveResult result = await WebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
        string receivedMessage = Encoding.UTF8.GetString(receiveBuffer.Array!, 0, result.Count);
        await HandlePacket(receivedMessage);
      }
    }
    finally
    {
      Connected = false;
    }
  }

  private async Task HandlePacket(string receivedMessage)
  {
    RoverReading? reading = JsonConvert.DeserializeObject<RoverReading>(receivedMessage);

    if (reading == null)
      return; // TODO @Helvece, log an error using your log management system

    await Task.Run(() =>
    {
      Readings.Add((DateTime.Now, reading));
      OnRoverReading?.Invoke(reading);
    });
  }
}