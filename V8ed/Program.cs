using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Managers;
using Vroumed.V8ed.Managers.Middlewares;
using Vroumed.V8ed.Models;
using Vroumed.V8ed.Models.Configuration;

namespace Vroumed.V8ed;

internal class Program
{
  private static void Main(string[] args)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    DependencyInjector dependencyInjector = new();
    builder.Services.AddSingleton(dependencyInjector);
    builder.Services.AddSingleton(new SessionManager());
    ServerConfiguration conf = builder.Configuration.GetSection(ServerConfiguration.SECTION_NAME).Get<ServerConfiguration>()!;

    dependencyInjector.CacheTransient<DatabaseManager>(() => new DatabaseManager(conf));
    dependencyInjector.CacheSingleton(dependencyInjector);

    DatabaseManager dbManager = dependencyInjector.Retrieve<DatabaseManager>();

    long count = Task.Run(async () => await dbManager.FetchOne<long>("SELECT COUNT(*) count FROM information_schema.tables WHERE table_schema = @schema;", new Dictionary<string, object>()
    {
      ["schema"] = conf.Database,
    })).Result!["count"];

    if (count == 0)
    {
      MigrationManager manager = new();

      dependencyInjector.Resolve(manager);
    }

    builder.Services.AddHttpContextAccessor();

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

    app.UseMiddleware<SessionMiddleware>();

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
  }
}