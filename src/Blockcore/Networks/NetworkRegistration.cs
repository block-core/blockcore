using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;

namespace Blockcore.Networks
{
    /// <summary>
    /// A container for storing/retrieving known networks.
    /// </summary>
    public static class NetworkRegistration
    {
        private static readonly ConcurrentDictionary<string, Network> RegisteredNetworks = new ConcurrentDictionary<string, Network>();

        /// <summary>
        /// Register an immutable <see cref="Network"/> instance so it is queryable through <see cref="GetNetwork"/> and <see cref="GetNetworks"/>.
        /// <para>
        /// If the network already exists, the already registered instance will be returned from the <see cref="RegisteredNetworks"/> collection.
        /// </para>
        /// </summary>
        public static Network Register(Network network)
        {
            Network existing = GetNetwork(network.Name);
            if (existing != null)
                return existing;

            IEnumerable<string> networkNames = network.AdditionalNames != null ? new[] { network.Name }.Concat(network.AdditionalNames) : new[] { network.Name };

            foreach (string networkName in networkNames)
            {
                if (string.IsNullOrEmpty(networkName))
                    throw new InvalidOperationException("A network name needs to be provided.");

                if (network.GetGenesis() == null)
                    throw new InvalidOperationException("A genesis block needs to be provided.");

                if (network.Consensus == null)
                    throw new InvalidOperationException("A consensus needs to be provided.");

                RegisteredNetworks.TryAdd(networkName.ToLowerInvariant(), network);
            }

            return network;
        }

        /// <summary>
        /// Clears the <see cref="RegisteredNetworks"/> collection.
        /// </summary>
        public static void Clear()
        {
            RegisteredNetworks.Clear();
        }

        /// <summary>
        /// Get network from name
        /// </summary>
        /// <param name="name">main,mainnet,testnet,test,testnet3,reg,regtest,seg,segnet</param>
        /// <returns>The network or null of the name does not match any network.</returns>
        public static Network GetNetwork(string name)
        {
            if (!RegisteredNetworks.Any())
                return null;

            return RegisteredNetworks.TryGet(name.ToLowerInvariant());
        }

        public static IEnumerable<Network> GetNetworks()
        {
            if (RegisteredNetworks.Any())
            {
                List<Network> others = RegisteredNetworks.Values.Distinct().ToList();

                foreach (Network network in others)
                    yield return network;
            }
        }

        internal static Network GetNetworkFromBase58Data(string base58, Base58Type? expectedType = null)
        {
            foreach (Network network in GetNetworks())
            {
                Base58Type? type = network.GetBase58Type(base58);
                if (type.HasValue)
                {
                    if (expectedType != null && expectedType.Value != type.Value)
                        continue;

                    return network;
                }
            }

            return null;
        }
    }
}