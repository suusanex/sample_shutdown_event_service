using Microsoft.Extensions.Hosting.WindowsServices;
using NLog.Extensions.Logging;
using sample_shutdown_event_service_worker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(log =>
    {
        log.AddNLog();
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.Configure<HostOptions>(option =>
        {
            option.ShutdownTimeout = TimeSpan.FromSeconds(60);
        });
        services.Configure<WindowsServiceLifetimeOptions>(option =>
        {
            option.ServiceName = "SampleShutdownService";
        });
        if (WindowsServiceHelpers.IsWindowsService())
        {
            services.AddSingleton<IHostLifetime, SampleServiceLifetime>();
        }
    })
    .Build();

await host.RunAsync();
