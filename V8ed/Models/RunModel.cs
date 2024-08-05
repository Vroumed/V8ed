using Vroumed.V8ed.Controllers;
using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("runs")]
public class Run : Crud
{

  #region Columns

  [CrudColumn("id", primaryKey: true, isAutoIncrement: true)]
  public int Id { get; set; }

  [CrudColumn("is_competitive", canBeNull: false)]
  public bool IsCompetitive { get; set; }

  [CrudColumn("is_auto", canBeNull: false)]
  public bool IsAuto { get; set; }

  [CrudColumn("estimated_distance", canBeNull: false)]
  public float EstimatedDistance { get; set; }

  [CrudColumn("video_url")]
  public string? VideoUrl { get; set; }

  #endregion

  #region Foreign Keys

  [CrudColumn("car", canBeNull: false)]
  public Car Car { get; set; } = null!;

  [CrudColumn("connection", canBeNull: false)]
  public Connection Connection { get; set; } = null!;

  #endregion

  #region Computed Enumerables

  [CrudEnumerableWhere("id", ComparisonType.EQUAL, "run_id")]
  public IEnumerable<Collision> Collisions { get; set; } = null!;

  [CrudEnumerableWhere("id", ComparisonType.EQUAL, "run_id")]
  public IEnumerable<OffRoadTracking> OffRoads { get; set; } = null!;

  #endregion
}