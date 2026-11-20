using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using SC4CleanitolAvalonia.Models;
using SC4CleanitolAvalonia.Services;
using SC4CleanitolAvalonia.ViewModels;
using SC4CleanitolAvalonia.Views;

namespace SC4CleanitolAvalonia;

public partial class App : Application {
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        //BindingPlugins.DataValidators.RemoveAt(0);

        var cfg = new ConfigService();
        var config = cfg.Load();

        var mw = new MainWindow();
        var fps = new FolderPickerService(mw);
        var cln = new CleanitolService();
        var ds = new DialogService(mw);
        var chk = new ChecksumService();
        DataContext = new MainViewModel(cln, fps, ds, cfg, config, chk);
        mw.DataContext = new MainViewModel(cln, fps, ds, cfg, config, chk);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = mw;
            desktop.Exit += (sender, e) => {
                cfg.Save(config);
            };
        } else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            singleViewPlatform.MainView = mw;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
