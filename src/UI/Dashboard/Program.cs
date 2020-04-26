using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Blockcore;
using System.Runtime.InteropServices;
using System.Diagnostics;

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

                    webBuilder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureServices(collection =>
                    {
                        if (services == null || fullNode == null)
                        {
                            return;
                        }

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
    }
}