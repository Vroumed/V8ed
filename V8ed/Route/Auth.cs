using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Managers;
using Vroumed.V8ed.Models;
using Vroumed.V8ed.Models.HttpModels;
using Vroumed.V8ed.Models.Users;

namespace Vroumed.V8ed.Controllers;

[ApiController]
[Route("auth")]
public class Auth : ControllerBase
{
  private readonly DependencyInjector _injector;
  private readonly HttpContext _context;
  public Auth(IHttpContextAccessor context, DependencyInjector injector)
  {
    _injector = injector;
    _context = context.HttpContext!;
  }

  public struct RoverApiResponse
  {
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("ready")]
    public bool Ready { get; set; }

    [JsonProperty("server_ready")]
    public bool ServerReady { get; set; }

    [JsonProperty("client_ready")]
    public bool ClientReady { get; set; }
  }

  [HttpPost]
  public async Task<IActionResult> Post([FromBody] RoverAuth login)
  {

    UserSession session = UserSession.FromContext(_context);
    if (session.Logged)
      return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "auth", "You are already connected"));

    IPAddress roverIP;
    try
    {
      roverIP = IPAddress.Parse(login.RoverIP);
    }
    catch (FormatException)
    {
      session.RoverManager.ConnectionAttempt++;
      return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "auth", "Provided ip was invalid"));
    }

    Uri apiUri = new($"http://{roverIP.ToString()}/wrover");
    HttpClient client = new();
    HttpResponseMessage response;

    try
    {
      response = await client.GetAsync(apiUri);
    }
    catch (HttpRequestException)
    {
      session.RoverManager.ConnectionAttempt++;
      return Conflict(this.GetStatusError(HttpStatusCode.Conflict, "rover-no-response", "Rover is not available"));
    }

    if (!response.IsSuccessStatusCode)
    {
      session.RoverManager.ConnectionAttempt++;
      return Conflict(this.GetStatusError(HttpStatusCode.Conflict, "rover-bad-response", "Rover is not available"));
    }

    string responseText = await response.Content.ReadAsStringAsync();
    RoverApiResponse roverApiResponse;

    try
    {
      roverApiResponse = JsonConvert.DeserializeObject<RoverApiResponse>(responseText);
    }
    catch
    {
      session.RoverManager.ConnectionAttempt++;
      return StatusCode(500, this.GetStatusError(HttpStatusCode.InternalServerError, "rover-error", "Could not read rover data"));
    }

    if (!roverApiResponse.ServerReady)
    {
      session.RoverManager.ConnectionAttempt++;
      return Conflict(this.GetStatusError(HttpStatusCode.Conflict, "rover-occupied", "Rover is not ready to accept server connections"));
    }

    session.RoverIP = roverIP;
    RoverManager.ConnectionStatus status = await session.RoverManager.Connect(new Uri($"ws://{roverIP.ToString()}/ws"), login.RoverKey);

    switch (status)
    {
      case RoverManager.ConnectionStatus.Ok:
        Dictionary<string, string> data = new()
        {
          { "token", session.SessionId }
        };

        Car car = new()
        {
          HardwareID = roverApiResponse.Id
        };

        _injector.Resolve(car);

        if (!car.ExistsInDatabase)
        {
          car.CarName = roverApiResponse.Name;
          car.Insert();
        }

        session.RoverManager.Car = car;

        session.RoverManager.Connection = new Connection()
        {
          TryCount = session.RoverManager.ConnectionAttempt, Time = DateTime.Now,
        };
        _injector.Resolve(session.RoverManager.Connection);

        session.RoverManager.Connection.Insert();

        return Ok(data);
      case RoverManager.ConnectionStatus.Busy:
        return Conflict(this.GetStatusError(HttpStatusCode.Conflict, "rover-occupied", "Rover is not ready to accept server connections"));
      case RoverManager.ConnectionStatus.WrongApiKey:
        return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "rover-passkey", "Rover received wrong credentials"));
      case RoverManager.ConnectionStatus.NoClient:
        return BadRequest(this.GetStatusError(HttpStatusCode.BadRequest, "rover-no-client", "Please connect client first"));
      default:
        throw new ArgumentOutOfRangeException();
    }
  }
}