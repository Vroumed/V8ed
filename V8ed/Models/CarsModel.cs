using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("Cars")]
public class Cars
{
  [CrudColumn("apiKey", primaryKey: true)]
  public string ApiKey { get; set; }

  [CrudColumn("carName")]
  public string CarName { get; set; }
}
