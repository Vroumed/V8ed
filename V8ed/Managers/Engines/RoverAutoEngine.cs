using System.ComponentModel.DataAnnotations;
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

  private Dictionary<int, int> SonarMap { get; } = new Dictionary<int, int>();
  public required RoverManager RoverManager { get; init; }
  public void Perform(RoverReading roverReading) { 
    //Main
  }

  #region Deplacement
  public void GoAhead() { }

  public void SlightRight() { }

  public void FullRight() { }

  public void FullLeft() { }

  public void SlightLeft() { }

  #endregion

  #region TakeData
  //45 , 68 , 90 , 113 , 135
  public void AimAt(int angle) { }

  #endregion

  #region Speed

  public void MaxSpeed() { }

  public void ConstantSpeed() { }

  public void MinSpeed() { }

  #endregion

}
