using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Managers;
using Vroumed.V8ed.Models.Configuration;

namespace Vroumed.V8ed;

internal class Program
{
  private static void Main(string[] args)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    DependencyInjector dependencyInjector = new();
    builder.Services.AddSingleton(dependencyInjector);
    ServerConfiguration conf = builder.Configuration.GetSection(ServerConfiguration.SECTION_NAME).Get<ServerConfiguration>()!;
    DatabaseManager DBmanager = new(conf);
    dependencyInjector.Cache(DBmanager);
    dependencyInjector.Cache(dependencyInjector);

    int count = Task.Run(async () => await DBmanager.FetchOne<int>("SELECT COUNT(*) count FROM information_schema.tables WHERE table_schema = @schema;", new Dictionary<string, object>()
    {
      ["schema"] = conf.Database,
    })).Result!["count"];

    if (count == 0)
    {
      MigrationManager manager = new();

      dependencyInjector.Resolve(manager);
    }

    // Add services to the container.
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    WebApplication app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }

    WebSocketOptions webSocketOptions = new()
    {
      KeepAliveInterval = TimeSpan.FromMinutes(2)
    };

    app.UseWebSockets(webSocketOptions);

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
  }
}