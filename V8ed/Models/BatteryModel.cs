using Vroumed.V8ed.Controllers;
using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("battery_event")]
public class EventBattery : Crud
{
  [CrudColumn("id", primaryKey: true, isAutoIncrement: true)]
  public int Id { get; set; }

  [CrudColumn("start_level")]
  public float StartLevel { get; set; }

  [CrudColumn("end_level")]
  public float EndLevel { get; set; }
}
