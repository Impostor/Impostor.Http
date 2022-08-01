namespace Impostor.Http;

using System.Net;
using Impostor.Api.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/** <summary>
 * Class registration class for Impostor.Http.
 * </summary>
 */
public class ImpostorHttpPluginStartup : IPluginStartup
{
    /// <inheritdoc/>
    public void ConfigureHost(IHostBuilder host)
    {
        HttpServerConfig config = CreateConfiguration().GetSection(HttpServerConfig.Section).Get<HttpServerConfig>() ?? new HttpServerConfig();

        host.ConfigureServices((host, services) =>
        {
            services.AddSingleton<HttpServerConfig>(config);
            services.AddSingleton<ListingManager>();
        });

        host.ConfigureWebHostDefaults(builder =>
        {
            builder.Configure(app =>
            {
                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            });

            builder.ConfigureKestrel((context, serverOptions) =>
            {
                serverOptions.Listen(IPAddress.Parse(config.ListenIp), config.ListenPort, listenOptions =>
                {
                    if (config.UseHttps)
                    {
                        listenOptions.UseHttps(config.CertificatePath);
                    }
                });
            });
        });
    }

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
    }

    private static IConfiguration CreateConfiguration()
    {
        ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.SetBasePath(Directory.GetCurrentDirectory());
        configurationBuilder.AddJsonFile("config_http.json", true);
        configurationBuilder.AddEnvironmentVariables(prefix: "BOOT_HTTP_");

        return configurationBuilder.Build();
    }
}
