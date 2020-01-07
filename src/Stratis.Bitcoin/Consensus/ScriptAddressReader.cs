﻿using NBitcoin;
using Stratis.Bitcoin.Interfaces;

namespace Stratis.Bitcoin.Consensus
{
    /// <inheritdoc cref="IScriptAddressReader"/>
    public class ScriptAddressReader : IScriptAddressReader
    {
        /// <inheritdoc cref="IScriptAddressReader.GetAddressFromScriptPubKey"/>
        public ScriptAddressResult GetAddressFromScriptPubKey(Network network, Script script)
        {
            ScriptTemplate scriptTemplate = network.StandardScriptsRegistry.GetTemplateFromScriptPubKey(script);

            var destinationAddress = new ScriptAddressResult();

            switch (scriptTemplate?.Type)
            {
                // Pay to PubKey can be found in outputs of staking transactions.
                case TxOutType.TX_PUBKEY:
                    PubKey pubKey = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(script);
                    destinationAddress.Address = pubKey.GetAddress(network).ToString();
                    break;
                // Pay to PubKey hash is the regular, most common type of output.
                case TxOutType.TX_PUBKEYHASH:
                    destinationAddress.Address = script.GetDestinationAddress(network).ToString();
                    break;
                case TxOutType.TX_SCRIPTHASH:
                    destinationAddress.Address = script.GetDestinationAddress(network).ToString();
                    break;
                case TxOutType.TX_COLDSTAKE:
                    destinationAddress = this.GetColdStakeAddresses(network, script);
                    break;
                case TxOutType.TX_NONSTANDARD:
                case TxOutType.TX_MULTISIG:
                case TxOutType.TX_NULL_DATA:
                case TxOutType.TX_SEGWIT:
                    break;
            }

            return destinationAddress;
        }

        public ScriptAddressResult GetColdStakeAddresses(Network network, Script script)
        {
            var destinationAddressResult = script.GetColdStakeDestinationAddress(network);
            return new ScriptAddressResult()
            {
                HotAddress = destinationAddressResult.hotAddress.ToString(),
                ColdAddress = destinationAddressResult.coldAddress.ToString()
            };
        }
    }
}