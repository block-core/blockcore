using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Blockcore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dashboard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args,
            FullNode fullNode = null,
            IEnumerable<ServiceDescriptor> services = null,
            DashboardSettings dashboardSettings = null) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    if (dashboardSettings != null)
                    {
                        webBuilder.UseUrls(dashboardSettings.DashboardUri.ToString());

#if DEBUG
                        Task.Run(() =>
                        {
                            Task.Delay(TimeSpan.FromSeconds(3)).Wait();
                            OpenBrowser(dashboardSettings.DashboardUri.ToString());
                        });
#endif
                    }

                    Console.WriteLine("GetCurrentDirectory " + Directory.GetCurrentDirectory());

                    webBuilder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureServices(collection =>
                    {
                        if (services == null || fullNode == null)
                        {
                            return;
                        }

                        collection.ConfigureOptions(typeof(EditorRCLConfigureOptions));

                        // copies all the services defined for the full node to the Api.
                        // also copies over singleton instances already defined
                        foreach (ServiceDescriptor service in services)
                        {
                            // open types can't be singletons
                            if (service.ServiceType.IsGenericType || service.Lifetime == ServiceLifetime.Scoped)
                            {
                                collection.Add(service);
                                continue;
                            }

                            object obj = fullNode.Services.ServiceProvider.GetService(service.ServiceType);
                            if (obj != null && service.Lifetime == ServiceLifetime.Singleton && service.ImplementationInstance == null)
                            {
                                collection.AddSingleton(service.ServiceType, obj);
                            }
                            else
                            {
                                collection.Add(service);
                            }
                        }
                    })

                    .UseStartup<Startup>();
                });

        public static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); // Works ok on windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);  // Works ok on linux
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url); // Not tested
            }
            else
            {
                // Do nothing
            }
        }

        /// <summary>
        /// This class will allow to read the wwwroot folder
        /// which has been set ad an embeded folder in to the dll (in the project file)
        /// </summary>
        internal class EditorRCLConfigureOptions : IPostConfigureOptions<StaticFileOptions>
        {
#pragma warning disable CS0618 // Type or member is obsolete

            private readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment environment;
#pragma warning restore CS0618 // Type or member is obsolete

            public EditorRCLConfigureOptions(Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
            {
                this.environment = environment;
            }

            public void PostConfigure(string name, StaticFileOptions options)
            {
                // Basic initialization in case the options weren't initialized by any other component
                options.ContentTypeProvider = options.ContentTypeProvider ?? new FileExtensionContentTypeProvider();

                if (options.FileProvider == null && this.environment.WebRootFileProvider == null)
                {
                    throw new InvalidOperationException("Missing FileProvider.");
                }

                options.FileProvider = options.FileProvider ?? this.environment.WebRootFileProvider;

                // Add our provider
                var filesProvider = new ManifestEmbeddedFileProvider(GetType().Assembly, "wwwroot");
                options.FileProvider = new CompositeFileProvider(options.FileProvider, filesProvider);
            }
        }
    }
}