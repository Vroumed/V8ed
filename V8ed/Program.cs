using QRCoder;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Managers;
using Vroumed.V8ed.Managers.Middlewares;
using Vroumed.V8ed.Models;
using Vroumed.V8ed.Models.Configuration;
using Vroumed.V8ed.Utils;
using Vroumed.V8ed.Utils.Logger;

namespace Vroumed.V8ed;

internal class Program
{
  private static void Main(string[] args)
  {

    Logger logger = new();

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    DependencyInjector dependencyInjector = new();
    builder.Services.AddSingleton(dependencyInjector);
    builder.Services.AddSingleton(new SessionManager());
    ServerConfiguration conf = builder.Configuration.GetSection(ServerConfiguration.SECTION_NAME).Get<ServerConfiguration>()!;

    dependencyInjector.CacheTransient<DatabaseManager>(() => new DatabaseManager(conf));
    dependencyInjector.CacheSingleton(dependencyInjector);
    dependencyInjector.CacheSingleton(logger);

    AssemblyConfigurationAttribute? assemblyConfigurationAttribute = typeof(Program).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
    string? buildConfigurationName = assemblyConfigurationAttribute?.Configuration;
    logger.Log(LogFile.Log, $"{nameof(V8ed)} ({buildConfigurationName}) : Startup");
    logger.Log(LogFile.Error, $"{nameof(V8ed)} ({buildConfigurationName})  : Startup");
    logger.Log(LogFile.Debug, $"{nameof(V8ed)} ({buildConfigurationName})  : Startup");
    logger.Log(LogFile.Log, new string('=', 32) + new string('\n', 5));
    logger.Log(LogFile.Error, new string('=', 32) + new string('\n', 5));
    logger.Log(LogFile.Debug, new string('=', 32) + new string('\n', 5));
    if (buildConfigurationName == "Debug")
    {
      logger.Log(LogFile.Log, "⚠️ Debug Mode Enabled ⚠️");
      logger.Log(LogFile.Debug, "⚠️ Debug Mode Enabled ⚠️");
    }

    AppDomain.CurrentDomain.UnhandledException += (_, args) =>
    {
      Exception exception = args.ExceptionObject as Exception;
      logger.Log(LogFile.Error, $"Unhandled exception: {exception?.Message ?? "Unknown"}");
      logger.Log(LogFile.Error, exception?.StackTrace ?? "No stack trace available");
      logger.Log(LogFile.Error, new string('=', 32));
      ExceptionDispatchInfo.Capture(exception!).Throw();

    };

    string? ip = NetworkUtil.GetLocalIPAddress();

    if (ip == null)
    {
      logger.Log(LogFile.Log, "No network interface found, exiting...");
      return;
    }

    QRCodeGenerator qrGenerator = new();
    QRCodeData qrCodeData = qrGenerator.CreateQrCode(ip, QRCodeGenerator.ECCLevel.Q);
    AsciiQRCode qrCode = new(qrCodeData);
    Console.WriteLine(qrCode.GetGraphic(1, "  ", "██"));

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