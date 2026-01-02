using Microsoft.Extensions.Logging;
using RestaurantManagementGUI.Helpers; // Để dùng HttpsClientHandlerService
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.ViewModels;
using RestaurantManagementGUI.Views;
using RestaurantManagementGUI.Views.Staff;
using CommunityToolkit.Maui;

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

        builder.Services.AddSingleton(sp =>
        {
            // Lấy Handler đã bypass SSL từ Bước 1
            var handler = HttpsClientHandlerService.GetPlatformMessageHandler();
            // Tự động gán BaseURL
            return new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };
        });

        // 3. ĐĂNG KÝ SERVICES
        builder.Services.AddSingleton(TCPSocketClient.Instance);
        builder.Services.AddSingleton<ChatService>();
        builder.Services.AddSingleton<TableService>();

        // 4. ĐĂNG KÝ VIEWMODELS
        builder.Services.AddTransient<ChefOrdersViewModel>();
        builder.Services.AddTransient<TablesViewModel>();
        builder.Services.AddTransient<FoodMenuViewModel>();
        builder.Services.AddTransient<BillGenerationViewModel>();
        builder.Services.AddTransient<ChatViewModel>();
        builder.Services.AddTransient<MenuViewerViewModel>();

        // 5. ĐĂNG KÝ PAGES (VIEWS)
        // Auth & Main
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ChefDashboardPage>();
        builder.Services.AddTransient<StaffDashboardPage>();

        // Features
        builder.Services.AddTransient<BillGenerationPage>();
        builder.Services.AddTransient<ChatPage>();
        builder.Services.AddTransient<FoodMenuPage>();
        builder.Services.AddTransient<ChefOrdersPage>();
        builder.Services.AddTransient<OrdersPage>();
        builder.Services.AddTransient<TablesPage>();
        builder.Services.AddTransient<QuanLyMonAnPage>();
        builder.Services.AddTransient<UsersPage>();
        builder.Services.AddTransient<RevenueReportPage>();
        builder.Services.AddTransient<ChefAndUserProfilePage>();

        // Các trang Edit/Add (Đăng ký để dùng DI bên trong nó)
        builder.Services.AddTransient<EditMonAnPage>();
        builder.Services.AddTransient<EditUserPage>();
        builder.Services.AddTransient<AddUserPage>();
        builder.Services.AddTransient<EditChefAndUserProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}