
using Dapplo.Microsoft.Extensions.Hosting.WinForms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Client;
using TrainCrewTIDWindow.Services;
using TrainCrewTIDWindow.Manager;

namespace TrainCrewTIDWindow {
    internal static class Program {

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main() {
            // IHostの初期化
            var host = new HostBuilder()
                .ConfigureLogging(options => options.AddDebug())
                .ConfigureServices(services => {
                    // DbContextの設定
                    services.AddDbContext<DbContext>(options => {
                        options.UseSqlite(
                            $"Filename={Path.Combine(Path.GetTempPath(), "trancrew-multiats-client.sqlite3")}");
                        options.UseOpenIddict();
                    });

                    // OpenIddictの設定
                    services.AddOpenIddict()

                        // Register the OpenIddict core components.
                        .AddCore(options => {
                            // Configure OpenIddict to use the Entity Framework Core stores and models.
                            // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                            options.UseEntityFrameworkCore()
                                .UseDbContext<DbContext>();
                        })

                        // Register the OpenIddict client components.
                        .AddClient(options => {
                            // Note: this sample uses the authorization code flow,
                            // but you can enable the other flows if necessary.
                            options.AllowAuthorizationCodeFlow()
                                .AllowRefreshTokenFlow();

                            // Register the signing and encryption credentials used to protect
                            // sensitive data like the state tokens produced by OpenIddict.
                            options.AddDevelopmentEncryptionCertificate()
                                .AddDevelopmentSigningCertificate();

                            // Add the operating system integration.
                            options.UseSystemIntegration();

                            // Register the System.Net.Http integration and use the identity of the current
                            // assembly as a more specific user agent, which can be useful when dealing with
                            // providers that use the user agent as a way to throttle requests (e.g Reddit).
                            options.UseSystemNetHttp()
                                .SetProductInformation(typeof(Program).Assembly);

                            // Add a client registration matching the client application definition in the server project.
                            options.AddRegistration(new OpenIddictClientRegistration {
                                Issuer = new Uri(ServerAddress.SignalAddress, UriKind.Absolute),

                                ClientId = "MultiATS_Client",
                                RedirectUri = new Uri("/", UriKind.Relative),

                            });
                        });

                    // Register the worker responsible for creating the database used to store tokens
                    // and adding the registry entries required to register the custom URI scheme.
                    //
                    // Note: in a real world application, this step should be part of a setup script.
                    services.AddHostedService<Worker>();

                })
                .ConfigureWinForms<TIDWindow>()
                .UseWinFormsLifetime()
                .Build();
            ApplicationConfiguration.Initialize();
            await host.RunAsync();
        }
    }
}