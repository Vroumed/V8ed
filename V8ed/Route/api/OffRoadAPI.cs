using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Dependencies.Attributes;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Managers;
using Vroumed.V8ed.Models;

namespace Vroumed.V8ed.Route.api;

[Route("offroad")]
[ApiController]
public class OffRoadAPI : ControllerBase
{
  private readonly DependencyInjector _injector;

  private DatabaseManager DatabaseManager { get; }

  public OffRoadAPI(DependencyInjector injector)
  {
    _injector = injector;
    DatabaseManager = injector.Retrieve<DatabaseManager>();
  }

  /// <summary>
  /// GetOffRoadById return a <see cref="OffRoadTracking"/> from an <paramref name="id"/>
  /// </summary>
  /// <param name="id">Id of the <see cref="OffRoadTracking"/></param>
  /// <returns></returns>
  [HttpGet]
  [SwaggerResponse(200, "Offroad", typeof(OffRoadTracking))]
  [SwaggerResponse(404, "Offroad with id '1' does not exist")]
  [Route("get/{id}")]
  public async Task<IActionResult> GetOffRoadById(int id)
  {
    OffRoadTracking offRoad = new()
    {
      Id = id
    };
    _injector.Resolve(offRoad);

    if (offRoad == null)
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Offroad with id '{id}' does not exist"));
    }

    return Ok(offRoad);
  }

  /// <summary>
  /// GetNumberOffRoadsByRun return a number of offroads from an Run id <paramref name="id"/>
  /// </summary>
  /// <param name="id">Id of the <see cref="Run"/></param>
  /// <returns></returns>
  [HttpGet]
  [SwaggerResponse(200, "number of offroads :", typeof(int))]
  [SwaggerResponse(404, "There is no offroads")]
  [Route("get/number/run/{id}")]
  public async Task<IActionResult> GetNumberOffRoadsByRun(int id)
  {
    int numberOffRoad = new();

    List<Dictionary<string, int>> rawOffRoads = await DatabaseManager.FetchAll<int>(
    "SELECT id FROM offroads Where run_id = @RunId;",
    new Dictionary<string, object>
    {
      ["RunId"] = id
    });

    IEnumerable<OffRoadTracking> listOffRoads = rawOffRoads.Select(row => new OffRoadTracking { Id = row["id"] });
    foreach (OffRoadTracking item in listOffRoads)
    {
      numberOffRoad++;
    }

    return Ok(numberOffRoad);
  }
}