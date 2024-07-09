using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("TeleMetricVideo")]
public class TeleMetricVideo
{
  [CrudColumn("id", primaryKey: true, isAutoIncrement: true)]
  public int Id { get; set; }

  [CrudColumn("IScompetitive")]
  public bool IScompetitive { get; set; }

  [CrudColumn("ISauto")]
  public bool ISauto { get; set; }
}