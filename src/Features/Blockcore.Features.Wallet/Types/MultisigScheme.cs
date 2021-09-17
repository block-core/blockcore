using Newtonsoft.Json;

namespace Blockcore.Features.Wallet.Types
{
    public class MultisigScheme
    {
        /// <summary>
        /// How many signatures will be suffient to move the funds.
        /// </summary>
        [JsonProperty(PropertyName = "threashold")]
        public int Threashold { get; set; }

        /// <summary>
        /// Cosigner extended pubkeys. Intentionally not including any xPriv at this stage as such model is simplest to start with.
        /// </summary>
        [JsonProperty(PropertyName = "xPubs")]
        public string[] XPubs { get; set; }
    }
}
