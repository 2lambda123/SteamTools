using Avalonia.Controls;
using BD.SteamClient.Services;
using BD.WTTS.Client.Resources;
using BD.WTTS.Converters;
using System.Collections.Specialized;
using static BD.WTTS.Services.INotificationService;

namespace BD.WTTS.UI;

partial class App
{
    public Dictionary<string, TrayMenuItem> TrayMenus { get; } = new Dictionary<string, TrayMenuItem>();

    public TrayIcons TrayIcons { get; init; } = new TrayIcons();

    readonly Dictionary<string, ICommand> trayIconMenus = new();

    public IReadOnlyDictionary<string, ICommand> TrayIconMenus => trayIconMenus;

    void InitTrayIcon()
    {
#if (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
        if (ApplicationLifetime is not
            IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
            return;

        var s = Startup.Instance;
        if (s.IsMainProcess)
        {
            s.HasTrayIcon = GeneralSettings.TrayIcon.Value;
            if (s.HasTrayIcon)
            {
                TrayIcon.SetIcons(this, TrayIcons);
                TrayIcons.Add(new TrayIcon
                {
                    Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://BD.WTTS.Client.Avalonia/UI/Assets/ApplicationIcon.ico"))),
                    ToolTipText = AssemblyInfo.Trademark,
                    Command = ReactiveCommand.Create(RestoreMainWindow),
                    //[!TrayIcon.IsVisibleProperty] = new Binding { Path = "Value", Source = GeneralSettings.IsEnableTrayIcon, Mode = BindingMode.OneWay },
                    Menu = new NativeMenu(),
                });

                UpdateMenuItems();

                //IViewModelManager.Instance.InitTaskBarWindowViewModel();
                //NotifyIconHelper.Init(this,
                //    notifyIconClick: (_, _) => RestoreMainWindow());
            }
            else
            {
                foreach (var trayIcon in TrayIcons)
                {
                    trayIcon.Dispose();
                }
                TrayIcons.Clear();
                TrayIcon.SetIcons(this, null);
                //NotifyIconHelper.Dispoe();
                //IViewModelManager.Instance.DispoeTaskBarWindowViewModel();
            }

            classicDesktopStyleApplicationLifetime.ShutdownMode =
#if UI_DEMO
                        ShutdownMode.OnMainWindowClose;
#else
                s.HasTrayIcon ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnMainWindowClose;
#endif

        }
#endif
    }

    public void UpdateMenuItems()
    {
#if (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
        MainThread2.BeginInvokeOnMainThread(() =>
        {
            var menus = TrayIcons.FirstOrDefault()?.Menu;

            if (menus != null)
            {
                menus.Items.Clear();

                var defaultTrayMenus = new List<NativeMenuItemBase>()
                    {
                        new NativeMenuItem
                        {
                            [!NativeMenuItem.HeaderProperty] = new MultiBinding {
                                Bindings = new List<Avalonia.Data.IBinding>()
                                {
                                    new Binding { Path = "IsRunningSteamProcess", Source = SteamConnectService.Current, Mode = BindingMode.OneWay },
                                    new Binding { Path = "Res.CloseSteam", Source = ResourceService.Current, Mode = BindingMode.OneWay },
                                    new Binding { Path = "Res.StartSteam", Source = ResourceService.Current, Mode = BindingMode.OneWay },
                                },
                                Converter = (VisbleStringConverter)App.Instance.FindResource("VisbleStringConverter")!,
                            },
                            Command = ReactiveCommand.Create(async () =>
                            {
                                if(ISteamService.Instance.IsRunningSteamProcess)
                                {
                                   await ISteamService.Instance.TryKillSteamProcess();
                                }
                                else
                                {
                                    ISteamService.Instance.StartSteamWithParameter();
                                }
                            }),
                        },
                        //new NativeMenuItemSeparator(),
                        new NativeMenuItem
                        {
                            [!NativeMenuItem.HeaderProperty] = new Binding { Path = "Res.OpenMainWindow", Source = ResourceService.Current, Mode = BindingMode.OneWay },
                            Command = ReactiveCommand.Create(RestoreMainWindow),
                        },
                        new NativeMenuItemSeparator(),
                        new NativeMenuItem
                        {
                            [!NativeMenuItem.HeaderProperty] = new Binding { Path = "Res.Exit", Source = ResourceService.Current, Mode = BindingMode.OneWay },
                            Command = ReactiveCommand.Create(() => { Shutdown(); })
                        },
                    };

                if (TrayMenus.Any_Nullable())
                {
                    foreach (var item in TrayMenus)
                    {
                        NativeMenu? subMenu = null;
                        if (item.Value.Items.Any_Nullable())
                        {
                            subMenu = new NativeMenu();
                            foreach (var sub in item.Value.Items)
                                subMenu.Add(new NativeMenuItem
                                {
                                    Header = sub.Name,
                                    Command = sub.Command,
                                    CommandParameter = sub.CommandParameter,
                                });
                            menus.Add(new NativeMenuItem
                            {
                                Header = item.Value.Name,
                                Menu = subMenu,
                            });
                        }
                        else
                        {
                            menus.Add(new NativeMenuItem
                            {
                                Header = item.Value.Name,
                                Command = item.Value.Command,
                                CommandParameter = item.Value.CommandParameter,
                            });
                        }
                    }
                    menus.Add(new NativeMenuItemSeparator());
                }

                foreach (var item in defaultTrayMenus)
                {
                    menus.Add(item);
                }
            }
        });
#endif
    }

    public void UpdateMenuItems(string menuKey, TrayMenuItem trayMenuItem)
    {
#if (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
        if (TrayMenus != null)
        {
            if (TrayMenus.ContainsKey(menuKey))
                TrayMenus[menuKey] = trayMenuItem;
            else
                TrayMenus.Add(menuKey, trayMenuItem);

            UpdateMenuItems();
        }
#endif
    }
}