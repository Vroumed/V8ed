using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Dependencies.Attributes;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Managers;
using Vroumed.V8ed.Models;

namespace Vroumed.V8ed.Route.api;

[Route("collision")]
[ApiController]
public class CollisionAPI : ControllerBase
{
  private readonly DependencyInjector _injector;

  private DatabaseManager DatabaseManager { get; }

  public CollisionAPI(DependencyInjector injector)
  {
    _injector = injector;
    DatabaseManager = injector.Retrieve<DatabaseManager>();
  }

  /// <summary>
  /// GetCollisionById return a <see cref="Collision"/> from an <paramref name="id"/>
  /// </summary>
  /// <param name="id">Id of the <see cref="Collision"/></param>
  /// <returns></returns>
  [HttpGet]
  [SwaggerResponse(200, "The collision", typeof(Collision))]
  [SwaggerResponse(404, "Collision whith id '1' does not exist")]
  [Route("get/{id}")]
  public async Task<IActionResult> GetCollisionById(int id)
  {
    Collision collision = new()
    {
      Id = id
    };
    _injector.Resolve(collision);

    if (collision.Id == null)
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Collision whith id '{id}' does not exist"));
    }

    return Ok(collision);
  }

  /// <summary>
  /// GetNumberCollisionByRun return a number of collision from an <paramref name="id"/>
  /// </summary>
  /// <param name="id">Id of the <see cref="Run"/></param>
  /// <returns></returns>
  [HttpGet]
  [SwaggerResponse(200, "number of collisions :", typeof(int))]
  [SwaggerResponse(404, "There is no collision")]
  [Route("get/number/run/{id}")]
  public async Task<IActionResult> GetNumberCollisionByRun(int id)
  {
    int numberCollision = new();

    List<Dictionary<string, int>> rawCollisions = await DatabaseManager.FetchAll<int>(
        "SELECT id FROM collisions Where run_id = @RunId;",
    new Dictionary<string, object>
    {
      ["RunId"] = id
    });

    IEnumerable<Collision> listCollisions = rawCollisions.Select(row => new Collision { Id = row["id"] });
    foreach (Collision item in listCollisions)
    {
      numberCollision++;
    }

    return Ok(numberCollision);
  }
}