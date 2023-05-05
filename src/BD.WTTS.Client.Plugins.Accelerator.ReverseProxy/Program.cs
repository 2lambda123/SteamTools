if (!args.Any())
    return (int)IPCExitCode.EmptyArrayArgs;
var pipeName = args[0];
if (string.IsNullOrWhiteSpace(pipeName))
    return (int)IPCExitCode.EmptyPipeName;

Ioc.ConfigureServices(ConfigureServices);

static void ConfigureServices(IServiceCollection services)
{
    services.AddLogging(l =>
    {
        l.AddConsole();
    });

    services.AddDnsAnalysisService();
    services.AddReverseProxyService();
    services.AddSingleton<ICertificateManager, CertificateManagerImpl>();
    services.AddSingleton<IPCService, IPCServiceImpl>();
}

using var ipc = IPCService.Instance;
var exitCode = await ipc.RunAsync(pipeName);
return exitCode;