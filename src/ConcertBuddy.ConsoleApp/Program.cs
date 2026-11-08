using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ConcertBuddy.ConsoleApp;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            await CreateHost(args).RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "The application failed");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    public static IHost CreateHost(string[] args, Action<IServiceCollection>? configureServices = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            // The server and launchSettings.json set this variable.
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        });

        builder.Logging.ClearProviders();
        builder.Services.AddConcertBuddy(builder.Configuration);
        configureServices?.Invoke(builder.Services);

        return builder.Build();
    }
}
