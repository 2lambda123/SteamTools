using BD.WTTS.Client.Resources;

namespace BD.WTTS.UI.Views.Pages;

public partial class ScriptPage : PageBase<ScriptPageViewModel>
{
    public ScriptPage()
    {
        InitializeComponent();
        DataContext = new ScriptPageViewModel();

        StoreButton.Click += StoreButton_Click;
    }

    private async void StoreButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var model = new ScriptStorePageViewModel();
        await IWindowManager.Instance.ShowTaskDialogAsync(model,
                       "脚本商店",
                       pageContent: new ScriptStorePage(),
                       isCancelButton: true);
    }
}