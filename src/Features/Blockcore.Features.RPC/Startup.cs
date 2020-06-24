﻿using System;
using System.IO;
using Blockcore.Utilities.JsonConverters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBitcoin;

namespace Blockcore.Features.RPC
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IObjectModelValidator, NoObjectModelValidator>();
            services.AddMvcCore(o =>
            {
                o.EnableEndpointRouting = false;
                o.ValueProviderFactories.Clear();
                o.ValueProviderFactories.Add(new RPCParametersValueProvider());
            })
                .AddNewtonsoftJson()
                .AddFormatterMappings();
            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, RPCJsonMvcOptionsSetup>());

            // We have added API versioning to the URLs of the version2-onwards controllers, so to not break routing we need this line.
            // Even though RPC will not actually use these endpoints.
            services.Configure<RouteOptions>(options => options.ConstraintMap.Add("apiVersion", typeof(ApiVersionRouteConstraint)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
#pragma warning disable CS0618 // Type or member is obsolete

        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
#pragma warning restore CS0618 // Type or member is obsolete
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            RpcSettings rpcSettings)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var fullNode = serviceProvider.GetService<FullNode>();

            var authorizedAccess = new RPCAuthorization();
            string cookieStr = "__cookie__:" + new uint256(RandomUtils.GetBytes(32));
            File.WriteAllText(fullNode.DataFolder.RpcCookieFile, cookieStr);
            authorizedAccess.Authorized.Add(cookieStr);
            if (rpcSettings.RpcPassword != null)
            {
                authorizedAccess.Authorized.Add(rpcSettings.RpcUser + ":" + rpcSettings.RpcPassword);
            }
            authorizedAccess.AllowIp.AddRange(rpcSettings.AllowIp);

            MvcNewtonsoftJsonOptions options = GetMVCOptions(serviceProvider);
            Serializer.RegisterFrontConverters(options.SerializerSettings, fullNode.Network);
            app.UseMiddleware(typeof(RPCMiddleware), authorizedAccess, rpcSettings.RPCContentType);
            app.UseRPC();
        }

        private static MvcNewtonsoftJsonOptions GetMVCOptions(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IOptions<MvcNewtonsoftJsonOptions>>().Value;
        }
    }

    internal class NoObjectModelValidator : IObjectModelValidator
    {
        public void Validate(ActionContext actionContext, ValidationStateDictionary validationState, string prefix, object model)
        {
        }
    }
}