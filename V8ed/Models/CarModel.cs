using Vroumed.V8ed.Controllers;
using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("cars")]
public class Car : Crud
{
  [CrudColumn("hwid", primaryKey: true)]
  public string HardwareID { get; set; } = string.Empty;

  [CrudColumn("car_name")]
  public string CarName { get; set; } = string.Empty;
}