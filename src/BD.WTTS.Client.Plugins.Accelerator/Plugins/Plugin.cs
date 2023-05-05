namespace BD.WTTS.Plugins;

#if (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
[CompositionExport(typeof(IPlugin))]
#endif
sealed class Plugin : PluginBase<Plugin>
{
    public override string Name => nameof(TabItemViewModel.TabItemId.Accelerator);

    public override void ConfigureDemandServices(IServiceCollection services, IApplication.IStartupArgs args, StartupOptions options)
    {
        services.TryAddScriptManager();
        if (options.IsTrace) StartWatchTrace.Record("DI.D.ScriptManager");

        if (options.HasHttpProxy)
        {
#if !DISABLE_ASPNET_CORE && (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
            // 通用 Http 代理服务
            services.AddSingleton<IReverseProxyService, IPCReverseProxyServiceImpl>();
            if (options.IsTrace) StartWatchTrace.Record("DI.D.HttpProxy");
#endif
        }

        if (options.HasServerApiClient)
        {
            // 添加仓储服务
            services.AddSingleton<IScriptRepository, ScriptRepository>();
        }
    }

    public override void ConfigureRequiredServices(IServiceCollection services, IApplication.IStartupArgs args, StartupOptions options)
    {
#if (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
        services.AddSingleton<IProxyService>(_ => ProxyService.Current);
#endif
    }

    public override ValueTask OnLoadedAsync()
    {
        return ValueTask.CompletedTask;
    }

    public override async ValueTask OnInitializeAsync()
    {
        if (ResourceService.IsChineseSimplified)
        {
            await ProxyService.Current.InitializeAsync();
        }
    }

    public override void OnAddAutoMapper(IMapperConfigurationExpression cfg)
    {
        cfg.AddProfile<AcceleratorAutoMapperProfile>();
    }

    public override async void OnUnhandledException(Exception ex, string name, bool? isTerminating = null)
    {
        IReverseProxyService reverseProxyService = Ioc.Get_Nullable<IReverseProxyService>();
        if (reverseProxyService != null)
        {
            await reverseProxyService.StartProxyAsync();
        }
        ProxyService.OnExitRestoreHosts();
    }
}