
using Microsoft.Extensions.Logging;
using RestaurantManagementGUI;
using RestaurantManagementGUI.Views;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.ViewModels;
using RestaurantManagementGUI.Views.Staff;

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

        // SERVICES
        builder.Services.AddSingleton(SocketListener.Instance);

        // VIEWMODELS
        builder.Services.AddSingleton<ChefOrdersViewModel>();
        builder.Services.AddTransient<TablesViewModel>();
        builder.Services.AddTransient<FoodMenuViewModel>();
        builder.Services.AddTransient<BillGenerationViewModel>();

        // PAGES
        builder.Services.AddSingleton<ChefOrdersPage>();
        builder.Services.AddTransient<TablesPage>();
        builder.Services.AddTransient<FoodMenuPage>();
        builder.Services.AddTransient<OrdersPage>();
        builder.Services.AddTransient<BillGenerationPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<QuanLyMonAnPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ChefDashboardPage>();
        builder.Services.AddTransient<StaffDashboardPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
