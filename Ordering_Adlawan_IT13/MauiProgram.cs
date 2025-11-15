using Microsoft.Extensions.Logging;
using Ordering_Adlawan_IT13.Services;

namespace Ordering_Adlawan_IT13
{
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
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddScoped<OrderingService>();
            builder.Services.AddSingleton<DatabaseInitializationService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            
            // Initialize database on app start
            _ = InitializeDatabaseAsync(app);
            
            return app;
        }
    
        private static async Task InitializeDatabaseAsync(MauiApp app)
        {
            try
            {
                var dbService = app.Services.GetRequiredService<DatabaseInitializationService>();
                Console.WriteLine("🔧 Initializing database...");
                await dbService.InitializeDatabaseAsync();
    
                bool connected = await dbService.TestConnectionAsync();
                if (connected)
                {
                    Console.WriteLine("✅ Database is ready to use!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Database initialization warning: {ex.Message}");
            }
        }
    }
}
