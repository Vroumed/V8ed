using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using Vroumed.V8ed.Extensions;

namespace Vroumed.V8ed.Controllers.api;

[Route("api/example")]
[ApiController]
public class ExampleApi : ControllerBase
{
  [HttpGet]
  [Route("get")]
  public async Task<IActionResult> TestGet()
  {
    await Task.Delay(new TimeSpan(1)); //using Async
    Dictionary<string, string> truc = new()
    {
      ["Ouah"] = "Une réponse !",
    };

    return Ok(JsonConvert.SerializeObject(truc));
  }

  [HttpGet]
  [Route("attribute")]
  public IActionResult TestAttribute(string parameter1)
  {
    if (parameter1.ToLower().Trim() != "test")
      return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, nameof(parameter1), $"{nameof(parameter1)} should be \"test\""));

    return NoContent();

  }
}