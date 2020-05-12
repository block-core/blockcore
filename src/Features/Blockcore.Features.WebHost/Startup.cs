using System;
using System.IO;
using System.Linq;
using Blockcore.Features.WebHost.Hubs;
using Blockcore.Utilities.JsonConverters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Blockcore.Features.WebHost
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IFullNode fullNode)
        {
            this.fullNode = fullNode;

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            this.Configuration = builder.Build();
        }

        private IFullNode fullNode;

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            WebHostSettings webHostSettings = fullNode.Services.ServiceProvider.GetService<WebHostSettings>();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(this.Configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });

            // Add service and create Policy to allow Cross-Origin Requests
            services.AddCors
            (
                options =>
                {
                    options.AddPolicy
                    (
                        "CorsPolicy",

                        builder =>
                        {
                            var allowedDomains = new[] { "http://localhost", "http://localhost:4200", "http://localhost:8080" };

                            builder
                            .WithOrigins(allowedDomains)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                        }
                    );
                });

            if (webHostSettings.EnableUI)
            {
                services.AddRazorPages();

                services.AddServerSideBlazor();

                services.Configure<RazorPagesOptions>(options =>
                {
                    // The UI elements moved under the UI folder
                    options.RootDirectory = "/UI/Pages";
                });
            }

            if (webHostSettings.EnableWS)
            {
                services.AddSignalR().AddNewtonsoftJsonProtocol(options =>
                {
                    var settings = new JsonSerializerSettings();
                    Serializer.RegisterFrontConverters(settings);
                    options.PayloadSerializerSettings = settings;
                });
            }

            if (webHostSettings.EnableAPI)
            {
                services.AddMvc(options =>
                {
                    options.Filters.Add(typeof(LoggingActionFilter));

#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
                    ServiceProvider serviceProvider = services.BuildServiceProvider();
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'

                    var apiSettings = (WebHostSettings)serviceProvider.GetRequiredService(typeof(WebHostSettings));
                    if (apiSettings.KeepaliveTimer != null)
                    {
                        options.Filters.Add(typeof(KeepaliveActionFilter));
                    }
                })
                // add serializers for NBitcoin objects
                .AddNewtonsoftJson(options => Serializer.RegisterFrontConverters(options.SerializerSettings))
                .AddControllers(this.fullNode.Services.Features, services);

                services.AddSwaggerGen(
                options =>
                {
                    string assemblyVersion = typeof(Startup).Assembly.GetName().Version.ToString();

                    options.SwaggerDoc("node",
                           new OpenApiInfo
                           {
                               Title = "Blockcore Node API",
                               Version = assemblyVersion,
                               Description = "Access to the Blockcore Node features.",
                               Contact = new OpenApiContact
                               {
                                   Name = "Blockcore",
                                   Url = new Uri("https://www.blockcore.net/")
                               }
                           });

                    string[] ApiXmlDocuments = new string[]
                    {
                            "Blockcore.xml",
                            "Blockcore.NBitcoin.xml",
                            "Blockcore.Features.WebHost.xml",
                            "Blockcore.Features.Wallet.xml",
                            "Blockcore.Features.RPC.xml",
                            "Blockcore.Features.Miner.xml",
                            "Blockcore.Features.MemoryPool.xml",
                            "Blockcore.Features.Consensus.xml",
                            "Blockcore.Features.BlockStore.xml",
                            "Blockcore.Features.ColdStaking.xml"
                    };

                    // Include XML documentation files.
                    string basePath = AppContext.BaseDirectory;

                    foreach (string xmlPath in ApiXmlDocuments.Select(xmlDocument => Path.Combine(basePath, xmlDocument)))
                    {
                        if (File.Exists(xmlPath))
                        {
                            options.IncludeXmlComments(xmlPath);
                        }
                    }

#pragma warning disable CS0618 // Type or member is obsolete
                    options.DescribeAllEnumsAsStrings();
#pragma warning restore CS0618 // Type or member is obsolete

                    // options.DescribeStringEnumsInCamelCase();
                });

                services.AddSwaggerGenNewtonsoftSupport(); // explicit opt-in - needs to be placed after AddSwaggerGen()
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            WebHostSettings webHostSettings = fullNode.Services.ServiceProvider.GetService<WebHostSettings>();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors("CorsPolicy");

            // Register this before MVC and Swagger.
            app.UseMiddleware<NoCacheMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                if (webHostSettings.EnableAPI)
                {
                    endpoints.MapControllers();
                }

                if (webHostSettings.EnableWS)
                {
                    endpoints.MapHub<EventsHub>("/events-hub");
                    endpoints.MapHub<NodeHub>("/ws");
                }

                if (webHostSettings.EnableUI)
                {
                    endpoints.MapBlazorHub();
                    endpoints.MapFallbackToPage("/_Host");
                }
            });

            if (webHostSettings.EnableAPI)
            {
                app.UseSwagger(c =>
                {
                    c.RouteTemplate = "docs/{documentName}/openapi.json";
                });

                app.UseSwaggerUI(c =>
                {
                    c.DefaultModelRendering(ModelRendering.Model);

                    app.UseSwaggerUI(c =>
                    {
                        c.RoutePrefix = "docs";
                        c.SwaggerEndpoint("/docs/node/openapi.json", "Blockcore Node API");
                    });
                });
            }
        }
    }
}