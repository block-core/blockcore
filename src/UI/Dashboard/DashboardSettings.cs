using System;
using System.Text;
using System.Timers;
using Blockcore.Configuration;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Dashboard
{
    /// <summary>
    /// Configuration related to the API interface.
    /// </summary>
    public class DashboardSettings
    {
        /// <summary>The default port used by the API when the node runs on the network.</summary>
        public const string DefaultDashboardHost = "http://localhost";

        private readonly ILogger logger;

        public Uri DashboardUri { get; set; }

        public int DashboardPort { get; set; }

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

        /// <summary>
        /// Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="nodeSettings">The node configuration.</param>
        public DashboardSettings(NodeSettings nodeSettings)
        {
            Guard.NotNull(nodeSettings, nameof(nodeSettings));

            this.logger = nodeSettings.LoggerFactory.CreateLogger(typeof(DashboardSettings).FullName);

            TextFileConfiguration config = nodeSettings.ConfigReader;

            this.UseHttps = config.GetOrDefault("usehttps", false);
            this.HttpsCertificateFilePath = config.GetOrDefault("certificatefilepath", (string)null);

            if (this.UseHttps && string.IsNullOrWhiteSpace(this.HttpsCertificateFilePath))
                throw new ConfigurationException("The path to a certificate needs to be provided when using https. Please use the argument 'certificatefilepath' to provide it.");

            var defaultApiHost = this.UseHttps
                ? DefaultDashboardHost.Replace(@"http://", @"https://")
                : DefaultDashboardHost;

            string dashboardHost = config.GetOrDefault("dashboarduri", defaultApiHost, this.logger);
            var dashboardUri = new Uri(dashboardHost);

            // for now default dashaboard to be as api port plust 1.
            int dashboardPort = config.GetOrDefault("dashboardport", nodeSettings.Network.DefaultAPIPort + 1, this.logger);

            // If no port is set in the API URI.
            if (dashboardUri.IsDefaultPort)
            {
                this.DashboardUri = new Uri($"{dashboardHost}:{dashboardPort}");
                this.DashboardPort = dashboardPort;
            }
            // If a port is set in the -apiuri, it takes precedence over the default port or the port passed in -apiport.
            else
            {
                this.DashboardUri = dashboardUri;
                this.DashboardPort = dashboardUri.Port;
            }
        }

        /// <summary>Prints the help information on how to configure the API settings to the logger.</summary>
        /// <param name="network">The network to use.</param>
        public static void PrintHelp(Network network)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"-dashboarduri=<string>                  URI to node's dashboard interface. Defaults to '{ DefaultDashboardHost }'.");
            builder.AppendLine($"-dashboardport=<0-65535>                Port of node's dashboard interface. Defaults to { network.DefaultAPIPort + 1 }.");

            NodeSettings.Default(network).Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            builder.AppendLine("####Dashboard Settings####");
            builder.AppendLine($"#URI to node's dashboard interface. Defaults to '{ DefaultDashboardHost }'.");
            builder.AppendLine($"#dashboarduri={ DefaultDashboardHost }");
            builder.AppendLine($"#Port of node's dashboard interface. Defaults to { network.DefaultAPIPort + 1 }.");
            builder.AppendLine($"#dashboardport={ network.DefaultAPIPort }");
        }
    }
}