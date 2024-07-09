using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("EventConnexion")]
public class EventConnexion
{
  [CrudColumn("id", primaryKey: true, isAutoIncrement: true)]
  public int Id { get; set; }

  [CrudColumn("try")]
  public bool Try { get; set; }

  [CrudColumn("satue")]
  public bool Satue { get; set; }
}