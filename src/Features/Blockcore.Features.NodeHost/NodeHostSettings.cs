using System;
using System.Text;
using System.Timers;
using Blockcore.Configuration;
using Blockcore.Networks;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.NodeHost
{
    /// <summary>
    /// Configuration related to the API interface.
    /// </summary>
    public class NodeHostSettings
    {
        /// <summary>The default port used by the API when the node runs on the network.</summary>
        public const string DefaultApiHost = "http://localhost";

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>URI to node's API interface.</summary>
        public Uri ApiUri { get; set; }

        /// <summary>Port of node's API interface.</summary>
        public int ApiPort { get; set; }

        /// <summary>
        /// If true then the node will add and start the Web Socket feature. This should never be enabled if node is accessible to the public.
        /// </summary>
        public bool EnableWS { get; private set; }

        /// <summary>
        /// If true the node will host a UI available in the NodeHost. This should never be enabled if node is accessible to the public.
        /// </summary>
        public bool EnableUI { get; private set; }

        /// <summary>
        /// If true the node will host a REST API in the NodeHost. This should never be enabled if node is accessible to the public.
        /// </summary>
        public bool EnableAPI { get; private set; }

        /// <summary>
        /// If true will require authentication on all sensitive APIs. Some APIs will be public available.
        /// </summary>
        public bool EnableAuth { get; private set; }

        /// <summary>
        /// The HTTPS certificate file path.
        /// </summary>
        /// <remarks>
        /// Password protected certificates are not supported. On MacOs, only p12 certificates can be used without password.
        /// Please refer to .Net Core documentation for usage: <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.-ctor?view=netcore-2.1#System_Security_Cryptography_X509Certificates_X509Certificate2__ctor_System_Byte___" />.
        /// </remarks>
        public string HttpsCertificateFilePath { get; set; }

        /// <summary>Use HTTPS or not.</summary>
        public bool UseHttps { get; set; }

        /// <summary>Use title from agent</summary>
        public string ApiTitle { get; set; }

        /// <summary>
        /// Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="nodeSettings">The node configuration.</param>
        public NodeHostSettings(NodeSettings nodeSettings)
        {
            Guard.NotNull(nodeSettings, nameof(nodeSettings));

            this.logger = nodeSettings.LoggerFactory.CreateLogger(typeof(NodeHostSettings).FullName);

            this.ApiTitle = "Blockcore-" + nodeSettings.Network.CoinTicker;

            TextFileConfiguration config = nodeSettings.ConfigReader;

            this.UseHttps = config.GetOrDefault("usehttps", false);
            this.HttpsCertificateFilePath = config.GetOrDefault("certificatefilepath", (string)null);

            if (this.UseHttps && string.IsNullOrWhiteSpace(this.HttpsCertificateFilePath))
                throw new ConfigurationException("The path to a certificate needs to be provided when using https. Please use the argument 'certificatefilepath' to provide it.");

            var defaultApiHost = this.UseHttps
                ? DefaultApiHost.Replace(@"http://", @"https://")
                : DefaultApiHost;

            string apiHost = config.GetOrDefault("apiuri", defaultApiHost, this.logger);
            var apiUri = new Uri(apiHost);

            // Find out which port should be used for the API.
            int apiPort = config.GetOrDefault("apiport", nodeSettings.Network.DefaultAPIPort, this.logger);

            // If no port is set in the API URI.
            if (apiUri.IsDefaultPort)
            {
                this.ApiUri = new Uri($"{apiHost}:{apiPort}");
                this.ApiPort = apiPort;
            }
            // If a port is set in the -apiuri, it takes precedence over the default port or the port passed in -apiport.
            else
            {
                this.ApiUri = apiUri;
                this.ApiPort = apiUri.Port;
            }

            this.EnableWS = config.GetOrDefault<bool>("enableWS", false, this.logger);
            this.EnableUI = config.GetOrDefault<bool>("enableUI", true, this.logger);
            this.EnableAPI = config.GetOrDefault<bool>("enableAPI", true, this.logger);
            this.EnableAuth = config.GetOrDefault<bool>("enableAuth", false, this.logger);
        }

        /// <summary>Prints the help information on how to configure the API settings to the logger.</summary>
        /// <param name="network">The network to use.</param>
        public static void PrintHelp(Network network)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"-apiuri=<string>                  URI to node's API interface. Defaults to '{ DefaultApiHost }'.");
            builder.AppendLine($"-apiport=<0-65535>                Port of node's API interface. Defaults to { network.DefaultAPIPort }.");
            builder.AppendLine($"-keepalive=<seconds>              Keep Alive interval (set in seconds). Default: 0 (no keep alive).");
            builder.AppendLine($"-usehttps=<bool>                  Use https protocol on the API. Defaults to false.");
            builder.AppendLine($"-certificatefilepath=<string>     Path to the certificate used for https traffic encryption. Defaults to <null>. Password protected files are not supported. On MacOs, only p12 certificates can be used without password.");
            builder.AppendLine($"-enableWS=<bool>                  Enable the Web Socket endpoints. Defaults to false.");
            builder.AppendLine($"-enableUI=<bool>                  Enable the node UI. Defaults to true.");
            builder.AppendLine($"-enableAPI=<bool>                 Enable the node API. Defaults to true.");
            builder.AppendLine($"-enableAuth=<bool>                Enable authentication on the node API. Defaults to true.");

            NodeSettings.Default(network).Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            builder.AppendLine("####API Settings####");
            builder.AppendLine($"#URI to node's API interface. Defaults to '{ DefaultApiHost }'.");
            builder.AppendLine($"#apiuri={ DefaultApiHost }");
            builder.AppendLine($"#Port of node's API interface. Defaults to { network.DefaultAPIPort }.");
            builder.AppendLine($"#apiport={ network.DefaultAPIPort }");
            builder.AppendLine($"#Use HTTPS protocol on the API. Default is false.");
            builder.AppendLine($"#usehttps=false");
            builder.AppendLine($"#Enable the Web Socket endpoints. Defaults to false.");
            builder.AppendLine($"#enableWS=false");
            builder.AppendLine($"#Enable the node UI. Defaults to true.");
            builder.AppendLine($"#enableUI=true");
            builder.AppendLine($"#Enable the node API. Defaults to true.");
            builder.AppendLine($"#enableAPI=true");
            builder.AppendLine($"#Enable authentication on the node API. Defaults to true.");
            builder.AppendLine($"#enableAuth=true");
            builder.AppendLine($"#Path to the file containing the certificate to use for https traffic encryption. Password protected files are not supported. On MacOs, only p12 certificates can be used without password.");
            builder.AppendLine(@"#Please refer to .Net Core documentation for usage: 'https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.-ctor?view=netcore-2.1#System_Security_Cryptography_X509Certificates_X509Certificate2__ctor_System_Byte___'.");
            builder.AppendLine($"#certificatefilepath=");
        }
    }
}