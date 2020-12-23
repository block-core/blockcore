using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Consensus;
using Blockcore.Networks;
using NBitcoin;

namespace Blockcore.Networks.City.Networks.Consensus
{
    public class CityPosConsensusOptions : PosConsensusOptions
    {
        /// <summary>Coinstake minimal confirmations softfork activation height for mainnet.</summary>
        public const int CityCoinstakeMinConfirmationActivationHeightMainnet = 500000;

        /// <summary>Coinstake minimal confirmations softfork activation height for testnet.</summary>
        public const int CityCoinstakeMinConfirmationActivationHeightTestnet = 15000;

        public override int GetStakeMinConfirmations(int height, Network network)
        {
            if (network.Name.ToLowerInvariant().Contains("test"))
            {
                return height < CityCoinstakeMinConfirmationActivationHeightTestnet ? 10 : 20;
            }

            // The coinstake confirmation minimum should be 50 until activation at height 500K (~347 days).
            return height < CityCoinstakeMinConfirmationActivationHeightMainnet ? 50 : 500;
        }
    }
}