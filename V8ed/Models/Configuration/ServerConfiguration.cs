namespace V8ed.Models.Configuration;

public class ServerConfiguration
{
  public const string SECTION_NAME = "Database";
  public string Server { get; set; } = "127.0.0.1";
  public string Database { get; set; } = "system";
  public string Username { get; set; } = "root";
  public string Password { get; set; } = "potato";
}