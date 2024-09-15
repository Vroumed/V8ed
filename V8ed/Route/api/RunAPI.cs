using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Managers;
using Vroumed.V8ed.Models;

namespace Vroumed.V8ed.Route.api;

[Route("run")]
[ApiController]
public class RunAPI : ControllerBase
{
  private readonly DependencyInjector _injector;

  private DatabaseManager DatabaseManager { get; }

  public RunAPI(DependencyInjector injector)
  {
    _injector = injector;
    DatabaseManager = injector.Retrieve<DatabaseManager>();
  }

  /// <summary>
  /// GetRunById get a run from an id
  /// </summary>
  /// <returns>Run.</returns>
  [HttpGet]
  [SwaggerResponse(200, "Run", typeof(Run))]
  [SwaggerResponse(404, "Run with id '1' does not exist")]
  [Route("get/{id}")]
  public async Task<IActionResult> GetRunById(int id)
  {
    Run run = new()
    {
      Id = id
    };
    _injector.Resolve(run);

    if (run.EstimatedDistance == 0)
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Run with id '{id}' does not exist"));
    }

    return Ok(run);
  }

  /// <summary>
  /// GetVideoByRunId get the video of a run
  /// </summary>
  /// <returns>Video of the run.</returns>
  [HttpGet]
  [SwaggerResponse(200, "The video of the run", typeof(string))]
  [SwaggerResponse(404, "Run with id '1' does not exist")]
  [Route("get/video/{id}")]
  public async Task<IActionResult> GetVideoByRunId(int id)
  {
    Run run = new()
    {
      Id = id
    };
    _injector.Resolve(run);

    if (string.IsNullOrEmpty(run.VideoUrl))
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Run with id '{id}' does not have a video"));
    }

    return Ok(run.VideoUrl);
  }

  /// <summary>
  /// GetRunsByCarId get all the runs for a car
  /// </summary>
  /// <returns>List of all <see cref="Run"/>.</returns>
  [HttpGet]
  [SwaggerResponse(200, "Runs", typeof(Run))]
  [SwaggerResponse(404, "Car with id '1' does not exist")]
  [Route("get/history/car/{id}")]
  public async Task<IActionResult> GetRunsByCarId(string id)
  {
    Car car = new()
    {
      HardwareID = id
    };
    _injector.Resolve(car);

    if (string.IsNullOrEmpty(car.CarName))
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Car with id '{id}' does not exist"));
    }

    List<Dictionary<string, int>> rawRuns = await DatabaseManager.FetchAll<int>(
        "SELECT id FROM Runs WHERE Car = @CarId;",
        new Dictionary<string, object>
        {
          ["CarId"] = id
        });

    IEnumerable<Run> list = rawRuns.Select(row => new Run { Id = row["id"] });
    foreach (Run item in list)
    {
      _injector.Resolve(item);
    }

    var result = new
    {
      Car = car,
      Runs = list
    };

    return Ok(result);
  }

  /// <summary>
  /// GetAllRuns get all the runs
  /// </summary>
  /// <returns>List of all <see cref="Run"/>.</returns>
  [HttpGet]
  [SwaggerResponse(200, "List of all runs", typeof(IEnumerable<Run>))]
  [Route("get/all")]
  public async Task<IActionResult> GetAllRuns()
  {
    List<Dictionary<string, int>> rawRuns = await DatabaseManager.FetchAll<int>(
        "SELECT id FROM Runs;");

    IEnumerable<Run> list = rawRuns.Select(row => new Run { Id = row["id"] });
    foreach (Run item in list)
    {
      _injector.Resolve(item);
    }

    return Ok(list);
  }

  /// <summary>
  /// GetEndRunInfo get all the information necessary for the end of the run display
  /// </summary>
  /// <returns>All the information for the end of the run.</returns>
  [HttpGet]
  [SwaggerResponse(200, "Get end run info", typeof(IEnumerable<Run>))]
  [Route("get/run/{id}/end")]
  public async Task<IActionResult> GetEndRunInfo(int id)
  {
    Run run = new()
    {
      Id = id
    };
    _injector.Resolve(run);

    Car car = new()
    {
      HardwareID = run.Car.HardwareID,
    };
    _injector.Resolve(car);

    int numberCollision = new();

    List<Dictionary<string, int>> rawCollisions = await DatabaseManager.FetchAll<int>(
        "SELECT id FROM collisions Where run_id = @RunId;",
    new Dictionary<string, object>
    {
      ["RunId"] = run.Id
    });

    IEnumerable<Collision> listCollisions = rawCollisions.Select(row => new Collision { Id = row["id"] });
    foreach (Collision item in listCollisions)
    {
      numberCollision++;
    }

    int numberOffRoad = new();

    if (run.IsAuto == true)
    {
      List<Dictionary<string, int>> rawOffRoads = await DatabaseManager.FetchAll<int>(
      "SELECT id FROM offroads Where run_id = @RunId;",
      new Dictionary<string, object>
      {
        ["RunId"] = run.Id
      });

      IEnumerable<OffRoadTracking> listOffRoads = rawOffRoads.Select(row => new OffRoadTracking { Id = row["id"] });
      foreach (OffRoadTracking item in listOffRoads)
      {
        numberOffRoad++;
      }
    }

    var result = new
    {
      Car = car,
      Run = run,
      Collisions = numberCollision,
      OffRoads = numberOffRoad
    };

    return Ok(result);
  }
}