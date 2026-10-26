using Application.Interface;
using Application.UseCase;
using Infrastructure.Configurations;
using Infrastructure.Repo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Presentation_WPF.ViewModels;
using Presentation_WPF.Views;
using System.IO;
using System.Windows;

namespace Presentation_WPF
{
    /// <summary>
    /// Application entry point and dependency injection configuration.
    /// Manages the application lifecycle and initializes all services and view models.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private IHost? _host;

        /// <summary>
        /// Initializes the application on startup by configuring the dependency injection container,
        /// loading Firebase settings from appsettings.json, and displaying the main window.
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<FirebaseSettings>(
                        context.Configuration.GetSection("Firebase"));

                    services.AddSingleton<IBlogPostRepo>(sp =>
                    {
                        var settings = sp.GetRequiredService<IOptions<FirebaseSettings>>().Value;
                        return new FirebaseRepository(settings.DatabaseUrl);
                    });

                    services.AddSingleton<IBlogPostService, BlogPostService>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            await _host.StartAsync();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            base.OnStartup(e);
        }

        /// <summary>
        /// Handles application shutdown by stopping and disposing the host instance.
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}