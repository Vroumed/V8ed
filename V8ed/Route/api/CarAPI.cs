using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Models;

namespace Vroumed.V8ed.Route.api;

[Route("car")]
[ApiController]
public class CarAPI : ControllerBase
{
  private readonly DependencyInjector _injector;

  public CarAPI(DependencyInjector injector)
  {
    _injector = injector;
  }

  /// <summary>
  /// GetCarById return a <see cref="Car"/> from an <paramref name="id"/>
  /// </summary>
  /// <param name="id">Id of the <see cref="Car"/></param>
  /// <returns></returns>
  [HttpGet]
  [SwaggerResponse(200, "The voiture", typeof(Car))]
  [SwaggerResponse(404, "Car whith id '1' does not exist")]
  [Route("get/{id}")]
  public async Task<IActionResult> GetCarById(string id)
  {
    Car car = new()
    {
      HardwareID = id
    };
    _injector.Resolve(car);

    if (string.IsNullOrEmpty(car.CarName))
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Car whith id '{id}' does not exist"));
    }

    return Ok(car);
  }

  /// <summary>
  /// DeleteCarById delete a <see cref="Car"/> from an <paramref name="id"/>
  /// </summary>
  /// <param name="id">Id of the <see cref="Car"/></param>
  /// <returns></returns>
  [HttpDelete]
  [SwaggerResponse(204)]
  [SwaggerResponse(404, "Car whith id '1' does not exist")]
  [Route("delete/{id}")]
  public async Task<IActionResult> DeleteCarById(string id)
  {
    Car car = new()
    {
      HardwareID = id
    };
    _injector.Resolve(car);

    if (string.IsNullOrEmpty(car.CarName))
    {
      return NotFound(this.GetStatusError(System.Net.HttpStatusCode.NotFound, nameof(id), $"Car whith id '{id}' does not exist"));
    }

    car.Delete();

    return NoContent();
  }
}
