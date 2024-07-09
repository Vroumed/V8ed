using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("TelemetricDistance")]
public class TelemetricDistance
{
  [CrudColumn("id", primaryKey: true, isAutoIncrement: true)]
  public int Id { get; set; }

  [CrudColumn("distanceEstimate")]
  public int DistanceEstimate { get; set; }

  [CrudColumn("IScompetitive")]
  public bool IScompetitive { get; set; }

  [CrudColumn("ISauto")]
  public bool ISauto { get; set; }
}
