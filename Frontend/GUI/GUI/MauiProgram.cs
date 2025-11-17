using Microsoft.Extensions.Logging;
using RestaurantManagementGUI;
using RestaurantManagementGUI.Views;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.ViewModels;

namespace RestaurantManagementGUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping("NoTitleBar", (handler, view) =>
        {
#if WINDOWS
            var nativeWindow = handler.PlatformView;
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow.TitleBar != null)
            {
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            }
#endif
        });

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("RobotoMono-Regular.ttf", "RobotoMono");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

        // Services registration
        builder.Services.AddSingleton<TableHubService>();   // single instance for SignalR hub
        builder.Services.AddSingleton<ApiService>();        // single API client
        builder.Services.AddTransient<TablesViewModel>();   // viewmodels as transient (new per page)
        builder.Services.AddTransient<TablesPage>();        // pages as transient
        builder.Services.AddTransient<FoodMenuViewModel>();
        builder.Services.AddTransient<FoodMenuPage>();

        builder.Logging.AddDebug();

        return builder.Build();
    }
}
