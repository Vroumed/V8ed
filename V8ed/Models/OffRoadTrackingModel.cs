using Vroumed.V8ed.Controllers;
using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("offroads")]
public class OffRoadTracking : Crud
{
  [CrudColumn("id", true, true)]
  public int Id { get; set; }

  [CrudColumn("run_id")]
  public int RunId { get; set; }

  [CrudColumn("time")]
  public DateTime Time { get; set; }
}