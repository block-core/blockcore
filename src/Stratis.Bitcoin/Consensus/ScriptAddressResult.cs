namespace Stratis.Bitcoin.Consensus
{
    public class ScriptAddressResult
    {
        public override string ToString()
        {
            return this.Address;
        }

        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(this.Address) &&
                string.IsNullOrEmpty(this.HotAddress) &&
                string.IsNullOrEmpty(this.ColdAddress);
        }

        public string Address { get; set; } = string.Empty;

        public string HotAddress { get; set; } = string.Empty;

        public string ColdAddress { get; set; } = string.Empty;
    }
}
