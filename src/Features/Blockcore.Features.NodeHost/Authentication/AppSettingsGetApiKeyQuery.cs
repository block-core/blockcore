using System.Linq;
using System.Threading.Tasks;
using Blockcore.Features.NodeHost.Settings;
using Microsoft.Extensions.Options;

namespace Blockcore.Features.NodeHost.Authentication
{
    public class AppSettingsGetApiKeyQuery : IGetApiKeyQuery
    {
        private BlockcoreSettings settings;

        public AppSettingsGetApiKeyQuery(IOptionsMonitor<BlockcoreSettings> options)
        {
            this.settings = options.CurrentValue;

            // Make sure it is possible to edit the API keys while running.
            options.OnChange(config =>
            {
                this.settings = config;
            });
        }

        public Task<ApiKey> Execute(string providedApiKey)
        {
            ApiKey key = this.settings.API.Keys.Where(key => key.Key == providedApiKey && key.Enabled ==  true).SingleOrDefault();
            return Task.FromResult(key);
        }
    }
}
