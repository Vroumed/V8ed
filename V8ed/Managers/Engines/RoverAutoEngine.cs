using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Text;
using Vroumed.V8ed.Models.Rover;

namespace Vroumed.V8ed.Managers.Engines;

public class RoverAutoEngine
{
  public float Speed { get; private set; } = 1f;
  enum LastTriggeredTracker
  {
    LeftTrack   = 0,
    MiddleTrack = 1,
    RightTrack  = 2,
  }
  private LastTriggeredTracker _lastTriggeredTracker;

  private Dictionary<int, int> SonarMap { get; } = new();
  public required RoverManager RoverManager { get; init; }
  public void Perform(RoverReading roverReading) { 
    //Main
  }

  private async Task SendCommandAsync(Command command)
  {
    string commandJson = JsonConvert.SerializeObject(command);
    byte[] message = Encoding.UTF8.GetBytes(commandJson);
    ArraySegment<byte> buffer = new(message);
    await RoverManager.WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
  }

  private readonly struct Command
  {
    [JsonProperty("cmd")]
    public required int CommandType { get; init; }

    [JsonProperty("data")]
    public required Dictionary<string, object> Data { get; init; }

    [JsonProperty("impersonate_client")]
#pragma warning disable IDE0051 // neds to be always sent via websocket
    private int ImpersonateClient { get; } = 1;
#pragma warning restore IDE0051
  }

  #region Deplacement

  public async Task GoAheadAsync()
  {
    Command command = new()
    {
      CommandType = 1,
      Data = new Dictionary<string, object>
                {
                    { "direction", 0 },
                    { "speed", Speed },
                    { "thrust", 1 }
                }
    };

    await SendCommandAsync(command);
  }

  public async Task SlightRightAsync()
  {
    Command command = new()
    {
      CommandType = 1,
      Data = new Dictionary<string, object>
                {
                    { "direction", 0.5 },
                    { "speed", Speed },
                    { "thrust", 1 }
                }
    };

    await SendCommandAsync(command);
  }

  public async Task FullRightAsync()
  {
    Command command = new()
    {
      CommandType = 1,
      Data = new Dictionary<string, object>
                {
                    { "direction", 1 },
                    { "speed", Speed },
                    { "thrust", 1 }
                }
    };

    await SendCommandAsync(command);
  }

  public async Task SlightLeftAsync()
  {
    Command command = new()
    {
      CommandType = 1,
      Data = new Dictionary<string, object>
                {
                    { "direction", -0.5 },
                    { "speed", Speed },
                    { "thrust", 100 }
                }
    };

    await SendCommandAsync(command);
  }

  public async Task FullLeftAsync()
  {
    Command command = new()
    {
      CommandType = 1,
      Data = new Dictionary<string, object>
                {
                    { "direction", -1 },
                    { "speed", Speed },
                    { "thrust", 100 }
                }
    };

    await SendCommandAsync(command);
  }

  #endregion

  #region TakeData

  // 45 , 68 , 90 , 113 , 135
  public async Task AimAtAsync(int angle)
  {
    if (angle is < 45 or > 135)
      throw new ArgumentOutOfRangeException(nameof(angle), "Angle must be between 45 and 135 degrees.");

    double normalizedAngle = (angle - 90) / 45.0;

    Command command = new()
    {
      CommandType = 2,
      Data = new Dictionary<string, object>
                {
                    { "headX", normalizedAngle },
                    { "headY", 0 }
                }
    };

    await SendCommandAsync(command);
  }

  #endregion

  #region Speed

  public void MaxSpeedAsync()
  {
    Speed = 1f;
  }

  public void ConstantSpeedAsync()
  {
    Speed = 0.5f;
  }

  public void MinSpeedAsync()
  {
    Speed = 0.2f;
  }

  #endregion

}
