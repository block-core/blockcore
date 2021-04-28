namespace Blockcore.Consensus
{
    /// <summary>
    /// The script result address.
    /// </summary>
    public class ScriptAddressResult
    {
        /// <summary>
        /// Will return the script public address if exists, Otherwise returns <see cref="string.Empty"/>
        /// </summary>
        public static implicit operator string(ScriptAddressResult scriptAddressResult)
        {
            return scriptAddressResult.Address.ToString();
        }

        /// <summary>
        /// If Address, Hot and Cold addresses are all empty, it will return true, otherwise false.
        /// </summary>
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(this.Address) &&
                string.IsNullOrEmpty(this.HotAddress) &&
                string.IsNullOrEmpty(this.ColdAddress);
        }

        /// <summary>
        /// Will return the script public address if exists, Otherwise returns <see cref="string.Empty"/>
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Will return the script hot public address if exists, Otherwise returns <see cref="string.Empty"/>
        /// </summary>
        public string HotAddress { get; set; } = string.Empty;

        /// <summary>
        /// Will return the script cold public address if exists, Otherwise returns <see cref="string.Empty"/>
        /// </summary>
        public string ColdAddress { get; set; } = string.Empty;
    }
}