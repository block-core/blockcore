using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Blockcore.Features.Api
{
    /// <summary>
    /// Configures the Swagger generation options.
    /// </summary>
    /// <remarks>This allows API versioning to define a Swagger document per API version after the
    /// <see cref="IApiVersionDescriptionProvider"/> service has been resolved from the service container.
    /// Adapted from https://github.com/microsoft/aspnet-api-versioning/blob/master/samples/aspnetcore/SwaggerSample/ConfigureSwaggerOptions.cs.
    /// </remarks>
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private const string ApiXmlFilename = "Blockcore.Api.xml";
        private const string WalletXmlFilename = "Blockcore.LightWallet.xml";

        private readonly IApiVersionDescriptionProvider provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
        /// </summary>
        /// <param name="provider">The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger documents.</param>
        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            this.provider = provider;
        }

        /// <inheritdoc />
        public void Configure(SwaggerGenOptions options)
        {
            // Add a swagger document for each discovered API version
            foreach (ApiVersionDescription description in this.provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }

            //Set the comments path for the swagger json and ui.
            string basePath = AppContext.BaseDirectory;
            string apiXmlPath = Path.Combine(basePath, ApiXmlFilename);
            string walletXmlPath = Path.Combine(basePath, WalletXmlFilename);

            if (File.Exists(apiXmlPath))
            {
                options.IncludeXmlComments(apiXmlPath);
            }

            if (File.Exists(walletXmlPath))
            {
                options.IncludeXmlComments(walletXmlPath);
            }

#pragma warning disable CS0618 // Type or member is obsolete
            options.DescribeAllEnumsAsStrings();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo()
            {
                Title = "Stratis Node API",
                Version = description.ApiVersion.ToString(),
                Description = "Access to the Stratis Node's core features."
            };

            if (info.Version.Contains("dev"))
            {
                info.Description += " This version of the API is in development and subject to change. Use an earlier version for production applications.";
            }

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}