using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("collisions")]
public class Collision
{
  [CrudColumn("run_id")]
  public int RunId { get; set; }

  [CrudColumn("time")]
  public DateTime Time { get; set; }
}
