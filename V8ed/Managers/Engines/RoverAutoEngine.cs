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
  private LastTriggeredTracker _lastTriggeredTracker = LastTriggeredTracker.MiddleTrack;

  private Dictionary<int, double> SonarMap { get; } = new();
  public required RoverManager RoverManager { get; init; }
  private bool _skipscan = false;
  private bool _avoidance = false;
  public async void Perform(RoverReading roverReading) 
  {


    bool reverse = true;

    if (reverse)
    {
      roverReading.TrackLeft = roverReading.TrackLeft == 1 ? 0 : 1;
      roverReading.TrackMiddle = roverReading.TrackMiddle == 1 ? 0 : 1;
      roverReading.TrackRight = roverReading.TrackRight == 1 ? 0 : 1;
    }


    await SonarScan(roverReading);

    if (!_avoidance)
      await LineReading(roverReading);
  }

  private async Task SonarScan(RoverReading roverReading)
  {
    int bef = SonarMap.Count;
    // 45 , 68 , 90 , 113 , 135
    switch (SonarMap.Count)
    {
      case 0:
        await AimAtAsync(68);
        break;
      case 1:
        await AimAtAsync(113);
        break;
    }


    SonarMap[(int)Math.Floor(roverReading.HeadX)] = roverReading.UltrasonicDistance;

    if (SonarMap.Count == bef)
    {
      //Something went wrong, reset
      SonarMap.Clear();
      _avoidance = false;
    }
  


    if (SonarMap.Count >= 2 && !_avoidance)
    {
      await EvaluateSonar();
      SonarMap.Clear();
    }
  }

  private async Task LineReading(RoverReading roverReading)
  {
    switch (roverReading)
    {
      case { TrackMiddle:1, TrackLeft: 0, TrackRight: 0 }:
        await GoAheadAsync();
        _lastTriggeredTracker = LastTriggeredTracker.MiddleTrack;
        break;
      case { TrackMiddle:0, TrackLeft: 1, TrackRight: 0 }:
        await SlightLeftAsync();
        _lastTriggeredTracker = LastTriggeredTracker.LeftTrack;
        break;
      case { TrackMiddle:0, TrackLeft: 0, TrackRight: 1 }:
        await SlightRightAsync();
        _lastTriggeredTracker = LastTriggeredTracker.RightTrack;
        break;

      case { TrackMiddle:0, TrackLeft: 0, TrackRight: 0 }:
        switch (_lastTriggeredTracker)
        {
          case LastTriggeredTracker.LeftTrack:
            await FullLeftAsync();
            break;
          case LastTriggeredTracker.MiddleTrack:
            await GoAheadAsync();
            break;
          case LastTriggeredTracker.RightTrack:
            await FullRightAsync();
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }

        break;

    }
  }

  private async Task EvaluateSonar()
  {
    KeyValuePair<int, double> min = SonarMap.MinBy(s => s.Value);
    if (min.Value < 30)
      switch (min.Key)
      {
        case >= 90:
          await FullRightAsync();
          _avoidance = true;
          Task.Run(async () =>
          {
            await Task.Delay(1500);
            _lastTriggeredTracker = LastTriggeredTracker.LeftTrack;
            _avoidance = false;
          });
          break;
        case < 90:
          await FullRightAsync();
          _avoidance = true;
          Task.Run(async () =>
          {
            await Task.Delay(1500);
            _lastTriggeredTracker = LastTriggeredTracker.RightTrack;
            _avoidance = false;
          });
          break;
      }
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
    public Command()
    {
    }

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
                    { "speed", 0.3f },
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
                    { "direction", -0.5 },
                    { "speed", 0.5f },
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
                    { "direction", -1 },
                    { "speed", 0.7f },
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
                    { "direction", 0.5 },
                    { "speed", 0.5f },
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
                    { "direction", 1 },
                    { "speed", 0.7f },
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
                    { "headY", -0.2f }
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
