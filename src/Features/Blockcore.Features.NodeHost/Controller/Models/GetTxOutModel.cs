using Blockcore.Consensus.Chain;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Networks;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonConverters;
using NBitcoin;
using Newtonsoft.Json;

namespace Blockcore.Controllers.Models
{
    /// <summary>
    /// A model returned by a gettxout request
    /// </summary>
    public class GetTxOutModel
    {
        public GetTxOutModel()
        {
        }

        /// <summary>
        /// Initializes a GetTxOutModel instance.
        /// </summary>
        /// <param name="unspentOutputs">The <see cref="UnspentOutput"/>.</param>
        /// <param name="network">The network the transaction occurred on.</param>
        /// <param name="tip">The current consensus tip's <see cref="ChainedHeader"/>.</param>
        public GetTxOutModel(UnspentOutput unspentOutputs, Network network, ChainedHeader tip)
        {
            this.BestBlock = tip.HashBlock;

            if (unspentOutputs?.Coins != null)
            {
                TxOut output = unspentOutputs.Coins.TxOut;
                this.Coinbase = unspentOutputs.Coins.IsCoinbase;
                this.Confirmations = NetworkExtensions.MempoolHeight == unspentOutputs.Coins.Height ? 0 : tip.Height - (int)unspentOutputs.Coins.Height + 1;
                this.Value = output.Value;
                this.ScriptPubKey = new ScriptPubKey(output.ScriptPubKey, network);
            }
        }

        /// <summary>The block hash of the consensus tip.</summary>
        [JsonProperty(Order = 0, PropertyName = "bestblock")]
        [JsonConverter(typeof(UInt256JsonConverter))]
        public uint256 BestBlock { get; set; }

        /// <summary>The number of confirmations for the unspent output.</summary>
        [JsonProperty(Order = 1, PropertyName = "confirmations")]
        public int Confirmations { get; set; }

        /// <summary>The value of the output.</summary>
        [JsonProperty(Order = 2, PropertyName = "value")]
        public Money Value { get; set; }

        /// <summary>The output's <see cref="ScriptPubKey"/></summary>
        [JsonProperty(Order = 3, PropertyName = "scriptPubKey")]
        public ScriptPubKey ScriptPubKey { get; set; }

        /// <summary>Boolean indicating if the unspent output is a coinbase transaction.</summary>
        [JsonProperty(Order = 4, PropertyName = "coinbase")]
        public bool Coinbase { get; set; }
    }
}
