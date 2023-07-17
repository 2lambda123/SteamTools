using Avalonia.Platform;
using AppResources = BD.WTTS.Client.Resources.Strings;

namespace BD.WTTS.UI.ViewModels;

public sealed partial class GameAccountPageViewModel
{
    public GameAccountPageViewModel()
    {
        GamePlatforms = new ObservableCollection<PlatformAccount>
        {
            new PlatformAccount(ThirdpartyPlatform.Steam)
            {
                FullName = "Steam",
                Icon = "Steam",
                DefaultExePath = SteamSettings.SteamProgramPath.Value,
            },
        };

        AddPlatformCommand = ReactiveCommand.Create<PlatformAccount>(AddPlatform);
        LoginNewCommand = ReactiveCommand.Create(LoginNewUser);
        SaveCurrentUserCommand = ReactiveCommand.Create(SaveCurrentUser);
        RefreshCommand = ReactiveCommand.Create(() => SelectedPlatform?.LoadUsers());

        LoadPlatforms();

        this.WhenAnyValue(x => x.SelectedPlatform)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(IsSelectedSteam));
            });
    }

    public void AddPlatform(PlatformAccount platform)
    {
        GamePlatforms?.Add(platform);
        AddGamePlatforms?.Remove(platform);
        GameAccountSettings.EnablePlatforms.Add(platform.FullName);
    }

    public void RemovePlatform(PlatformAccount platform)
    {
        if (platform.FullName == "Steam")
        {
            Toast.Show(ToastIcon.Warning, AppResources.Warning_PlatformDeletionNotAllowed_.Format(platform.FullName));
            return;
        }
        AddGamePlatforms?.Add(platform);
        GamePlatforms?.Remove(platform);
        GameAccountSettings.EnablePlatforms.Remove(platform.FullName);
    }

    IEnumerable<PlatformAccount>? GetSupportPlatforms()
    {
        if (AssetLoader.Exists(PlatformsPath))
        {
            var stream = AssetLoader.Open(PlatformsPath);
            if (stream == null) return null;

            var platforms = JsonSerializer.Deserialize<PlatformAccount[]>(stream);
            if (platforms == null) return null;

            foreach (var platform in platforms)
            {
                if (platform.ClearPaths?.Contains("SAME_AS_LOGIN_FILES") == true && platform.LoginFiles != null)
                {
                    platform.ClearPaths = platform.LoginFiles.Keys.ToList();
                }
                if (GameAccountSettings.PlatformSettings.ContainsKey(platform.FullName))
                {
                    platform.PlatformSetting = GameAccountSettings.PlatformSettings.Value?[platform.FullName];
                }
            }
            return platforms;
        }
        return null;
    }

    public void LoadPlatforms()
    {
        GamePlatforms?[0].LoadUsers();

        var temp = GetSupportPlatforms();
        if (temp != null)
        {
            if (GameAccountSettings.EnablePlatforms.Any_Nullable())
            {
                AddGamePlatforms = new ObservableCollection<PlatformAccount>();

                foreach (var p in temp)
                {
                    if (GameAccountSettings.EnablePlatforms.Contains(p.FullName))
                    {
                        if (GamePlatforms?.Contains(p) == false)
                        {
                            GamePlatforms.Add(p);
                            p.LoadUsers();
                        }
                    }
                    else
                    {
                        AddGamePlatforms.Add(p);
                    }
                }
            }
            else
            {
                AddGamePlatforms = new ObservableCollection<PlatformAccount>(temp);
            }
        }
    }

    async void SaveCurrentUser()
    {
        if (SelectedPlatform == null) return;
        var textModel = new TextBoxWindowViewModel();
        var result = await IWindowManager.Instance.ShowTaskDialogAsync(textModel, AppResources.Title_AddAccount_.Format(SelectedPlatform.FullName), subHeader: AppResources.Title_PleaseInputCurrentAccountName_.Format(SelectedPlatform.FullName), isCancelButton: true);
        if (result)
        {
            if (string.IsNullOrEmpty(textModel.Value))
            {
                Toast.Show(ToastIcon.Warning, AppResources.Warning_PleaseInputAccountName);
                return;
            }
            if (SelectedPlatform.CurrnetUserAdd(textModel.Value))
            {
                Toast.Show(ToastIcon.Success, AppResources.Success_SavedSuccessfully_.Format(textModel.Value));
                SelectedPlatform.LoadUsers();
            }
        }
    }

    async void LoginNewUser()
    {
        if (SelectedPlatform == null) return;
        var textModel = new MessageBoxWindowViewModel
        {
            Content = AppResources.ModelContent_LoginNewUser
        };
        var result = await IWindowManager.Instance.ShowTaskDialogAsync(textModel, AppResources.Title_LoginAccount_.Format(SelectedPlatform.FullName), isCancelButton: true);
        if (result)
            SelectedPlatform?.CurrnetUserAdd(null);
    }
}
