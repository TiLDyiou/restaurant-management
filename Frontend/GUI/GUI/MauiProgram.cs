using Microsoft.Extensions.Logging;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI;

namespace RestaurantManagementGUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("RobotoMono-Regular.ttf", "RobotoMono");
                });

            builder.Logging.AddDebug();
            builder.Services.AddTransient<FoodMenuViewModel>();
            builder.Services.AddTransient<FoodMenuPage>();
        return builder.Build();
    }
}