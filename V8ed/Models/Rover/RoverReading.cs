using Newtonsoft.Json;

namespace Vroumed.V8ed.Models.Rover;

public class RoverReading
{
  [JsonProperty("battery_voltage")]
  public double BatteryVoltage { get; set; }

  [JsonProperty("photosensitive")]
  public int Photosensitive { get; set; }

  [JsonProperty("track_left")]
  public int TrackLeft { get; set; }

  [JsonProperty("track_middle")]
  public int TrackMiddle { get; set; }

  [JsonProperty("track_right")]
  public int TrackRight { get; set; }

  [JsonProperty("ultrasonic_di")]
  public double UltrasonicDistance { get; set; } // On suppose que c'est une distance

  [JsonProperty("speed")]
  public int Speed { get; set; }

  [JsonProperty("direction")]
  public int Direction { get; set; }

  [JsonProperty("headX")]
  public int HeadX { get; set; }

  [JsonProperty("thrust")]
  public int Thrust { get; set; }
}