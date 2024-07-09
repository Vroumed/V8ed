using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("EventShock")]
public class EventShock
{
  [CrudColumn("id", primaryKey: true, isAutoIncrement: true)]
  public int Id { get; set; }

  [CrudColumn("event")]
  public bool Event { get; set; }

  [CrudColumn("Scompetitive")]
  public bool Scompetitive { get; set; }

  [CrudColumn("ISauto")]
  public bool ISauto { get; set; }
}
