namespace StudentsJson.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = new MainPage();
        var window = new Window(new NavigationPage(page)) { Title = "Успішність студентів (ЛР3, варіант 6)" };

#if WINDOWS
        window.HandlerChanged += (_, _) =>
        {
            if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            {
                nativeWindow.AppWindow.Closing += async (_, args) =>
                {
                    args.Cancel = true;
                    if (await page.ConfirmExitAsync())
                        Current?.Quit();
                };
            }
        };
#endif
        return window;
    }
}
