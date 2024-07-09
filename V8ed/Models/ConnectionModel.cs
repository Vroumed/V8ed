using Vroumed.V8ed.Controllers;
using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("connections")]
public class Connection : Crud
{
  [CrudColumn("id", primaryKey: true, isAutoIncrement: true)]
  public int Id { get; set; }

  [CrudColumn("try_count", canBeNull:false, @default:0)]
  public int TryCount { get; set; }
}