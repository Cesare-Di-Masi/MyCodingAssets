using CommunityToolkit.Maui;
using MauiIcons.FontAwesome.Solid;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

namespace BattagliaNavale
{
    //punto di ingresso dell'applicazione, qui viene costruita e ne vengono configurati vari aspetti
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                // Initialize the .NET MAUI Community Toolkit by adding the below line of code
                .UseMauiCommunityToolkit()
                .UseFontAwesomeSolidMauiIcons()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton(AudioManager.Current);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}