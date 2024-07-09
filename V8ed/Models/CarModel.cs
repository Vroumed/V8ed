﻿using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Models;

[CrudTable("cars")]
public class Car
{
  [CrudColumn("api_key", primaryKey: true)]
  public string ApiKey { get; set; }

  [CrudColumn("car_name")]
  public string CarName { get; set; }
}