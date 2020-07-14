using System;
using System.IO;
using System.Linq;
using Blockcore.Broadcasters;
using Blockcore.Features.NodeHost.Events;
using Blockcore.Features.NodeHost.Hubs;
using Blockcore.Utilities.JsonConverters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerUI;
using BlazorModal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Blockcore.Features.NodeHost.Authentication;
using Blockcore.Features.NodeHost.Authorization;
using Blockcore.Features.NodeHost.Settings;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Blockcore.Features.NodeHost
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
            NodeHostSettings hostSettings = fullNode.Services.ServiceProvider.GetService<NodeHostSettings>();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(this.Configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });

            services.Configure<BlockcoreSettings>(this.Configuration.GetSection("Blockcore"));

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

            if (hostSettings.EnableAuth)
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                    options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                })
                .AddApiKeySupport(options => { });

                services.AddSingleton<IAuthorizationHandler, OnlyUsersAuthorizationHandler>();
                services.AddSingleton<IAuthorizationHandler, OnlyAdminsAuthorizationHandler>();

                services.AddSingleton<IGetApiKeyQuery, AppSettingsGetApiKeyQuery>();
            }

            if (hostSettings.EnableUI)
            {
                services.AddRazorPages();

                services.AddServerSideBlazor();

                services.Configure<RazorPagesOptions>(options =>
                {
                    // The UI elements moved under the UI folder
                    options.RootDirectory = "/UI/Pages";
                });

                services.AddBlazorModal();
            }

            if (hostSettings.EnableWS)
            {
                services.AddSignalR().AddNewtonsoftJsonProtocol(options =>
                {
                    var settings = new JsonSerializerSettings();

                    settings.Error = (sender, args) =>
                    {
                        args.ErrorContext.Handled = true;
                    };

                    Serializer.RegisterFrontConverters(settings);
                    options.PayloadSerializerSettings = settings;
                    options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                });
            }

            if (hostSettings.EnableAPI)
            {
                services.AddMvc(options =>
                {
                    options.Filters.Add(typeof(LoggingActionFilter));
                })
                // add serializers for NBitcoin objects
                .AddNewtonsoftJson(options =>
                {
                    Serializer.RegisterFrontConverters(options.SerializerSettings);
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                .AddControllers(this.fullNode.Services.Features, services);

                services.AddSwaggerGen(
                options =>
                {
                    string assemblyVersion = typeof(Startup).Assembly.GetName().Version.ToString();

                    string description = "Access to the Blockcore Node features.";

                    if (hostSettings.EnableAuth)
                    {
                        description += " Authorization is enabled on this API. You must have API key to perform calls that are not public.";

                        options.AddSecurityDefinition(ApiKeyConstants.HeaderName, new OpenApiSecurityScheme
                        {
                            Description = "API key needed to access the endpoints. Node-Api-Key: YOUR_KEY",
                            In = ParameterLocation.Header,
                            Name = ApiKeyConstants.HeaderName,
                            Type = SecuritySchemeType.ApiKey
                        });

                        options.AddSecurityRequirement(new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Name = ApiKeyConstants.HeaderName,
                                    Type = SecuritySchemeType.ApiKey,
                                    In = ParameterLocation.Header,
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = ApiKeyConstants.HeaderName
                                    },
                                 },
                                 new string[] {}
                             }
                        });
                    }

                    options.SwaggerDoc("node",
                           new OpenApiInfo
                           {
                               Title = hostSettings.ApiTitle + " Node API",
                               Version = assemblyVersion,
                               Description = description,
                               Contact = new OpenApiContact
                               {
                                   Name = "Blockcore",
                                   Url = new Uri("https://www.blockcore.net/")
                               }
                           });

                    SwaggerApiDocumentationScaffolder.Scaffold(options);

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
            NodeHostSettings hostSettings = fullNode.Services.ServiceProvider.GetService<NodeHostSettings>();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors("CorsPolicy");

            // Register this before MVC and Swagger.
            app.UseMiddleware<NoCacheMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                if (hostSettings.EnableAPI)
                {
                    if (hostSettings.EnableAuth)
                    {
                        endpoints.MapControllers();
                    }
                    else
                    {
                        // When authentication is not enabled, we must set this filter.
                        endpoints.MapControllers().WithMetadata(new AllowAnonymousAttribute());
                    }
                }

                if (hostSettings.EnableWS)
                {
                    IHubContext<EventsHub> hubContext = app.ApplicationServices.GetService<IHubContext<EventsHub>>();
                    IEventsSubscriptionService eventsSubscriptionService = fullNode.Services.ServiceProvider.GetService<IEventsSubscriptionService>();
                    eventsSubscriptionService.SetHub(hubContext);

                    endpoints.MapHub<EventsHub>("/ws-events");
                    endpoints.MapHub<NodeHub>("/ws");
                }

                if (hostSettings.EnableUI)
                {
                    endpoints.MapBlazorHub();
                    endpoints.MapFallbackToPage("/_Host");
                }
            });

            if (hostSettings.EnableAPI)
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