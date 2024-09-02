using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Models;

namespace Vroumed.V8ed.Route.api;

[Route("run")]
[ApiController]
public class RunAPI : ControllerBase
{
  private readonly DependencyInjector _injector;

  public RunAPI(DependencyInjector injector)
  {
    _injector = injector;
  }

  [HttpGet]
  [SwaggerResponse(200, "The run", typeof(Run))]
  [SwaggerResponse(404, "Run whith id '1' does not exist")]
  [Route("get/{id}")]
  public async Task<IActionResult> GetRunById(string idString)
  {
    int id = int.Parse(idString);

    Run run = new()
    {
      Id = id
    };
    _injector.Resolve(run);

    if (run.EstimatedDistance == 0)
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Run whith id '{id}' does not exist"));
    }

    return Ok(run);
  }

  [HttpGet]
/*  [SwaggerResponse(200, "The video of the run", typeof(Run.VideoUrl))]
*/  [SwaggerResponse(404, "Run whith id '1' does not exist")]
  [Route("get/video/{id}")]
  public async Task<IActionResult> GetVideoByRunId(string idString)
  {
    int id = int.Parse(idString);

    Run run = new()
    {
      Id = id
    };
    _injector.Resolve(run);

    if (string.IsNullOrEmpty(run.VideoUrl))
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Run whith id '{id}' does not have a video"));
    }

    return Ok(run.VideoUrl);
  }

  [HttpGet]
  [SwaggerResponse(200, "The collisions of the run", typeof(Collision))]
  [SwaggerResponse(404, "Run whith id '1' does not exist")]
  [Route("get/collision/{id}")]
  public async Task<IActionResult> GetCollisionByRun(string idString)
  {
    int id = int.Parse(idString);

    List<Collision> collisions = _injector.RetrieveAll<Collision>().Where(c => c.RunId == id).ToList();

    if (collisions == null || collisions.Count == 0)
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"No collision with Run id '{id}' were found"));
    }

    return Ok(collisions);
  }

  [HttpGet]
  [SwaggerResponse(200, "number of collision by run", typeof(int))]
  [SwaggerResponse(404, "Run whith id '1' does not exist")]
  [Route("get/collision/count/{id}")]
  public async Task<IActionResult> GetNumberCollisionByRunId(string idString)
  {
    int id = int.Parse(idString);

    List<Collision> collisions = _injector.RetrieveAll<Collision>().Where(c => c.RunId == id).ToList();

    if (collisions == null || collisions.Count == 0)
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"No collision with Run id '{id}' were found"));
    }

    return Ok(collisions.Count);
  }

  [HttpGet]
  [SwaggerResponse(200, "The offroad of the run", typeof(OffRoadTracking))]
  [SwaggerResponse(404, "Run whith id '1' does not exist")]
  [Route("get/offroad/{id}")]
  public async Task<IActionResult> GetOffroadByRun(string idString)
  {
    int id = int.Parse(idString);

    List<OffRoadTracking> offroads = _injector.RetrieveAll<OffRoadTracking>().Where(of => of.RunId == id).ToList();

    if (offroads == null || offroads.Count == 0)
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"No offroad with Run id '{id}' were found"));
    }

    return Ok(offroads);
  }

  [HttpGet]
  [SwaggerResponse(200, "number of offroads by run", typeof(int))]
  [SwaggerResponse(404, "Run whith id '1' does not exist")]
  [Route("get/offroad/count/{id}")]
  public async Task<IActionResult> GetNumberOffroadByRunId(string idString)
  {
    int id = int.Parse(idString);

    List<OffRoadTracking> offroads = _injector.RetrieveAll<OffRoadTracking>().Where(of => of.RunId == id).ToList();

    if (offroads == null || offroads.Count == 0)
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"No offroad with Run id '{id}' were found"));
    }

    return Ok(offroads.Count);
  }

  [HttpGet]
  [SwaggerResponse(200, "The run", typeof(Run))]
  [SwaggerResponse(404, "Run whith id '1' does not exist")]
  [Route("get/history/car/{id}")]
  public async Task<IActionResult> GetRunsByCarId(int id)
  {
    Run run = new()
    {
      Id = id
    };

    _injector.Resolve(run);

    if (run.EstimatedDistance == 0)
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Run whith id '{id}' does not exist"));
    }

    return Ok(run);
  }

  /// <summary>
  /// Get all runs.
  /// </summary>
  /// <returns>List of all <see cref="Run"/>.</returns>
  [HttpGet]
  [SwaggerResponse(200, "List of all runs", typeof(IEnumerable<Run>))]
  [Route("get/all")]
  public async Task<IActionResult> GetAllRuns()
  {
    var runs = _injector.RetrieveAll<Run>().ToList();

    if (runs == null || !runs.Any())
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, "Runs", "No run found"));
    }

    return Ok(runs);
  }
}