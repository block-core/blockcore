using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Base;
using Blockcore.Broadcasters;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Configuration;
using Blockcore.Configuration.Logging;
using Blockcore.Configuration.Settings;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Features.BlockStore;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.Miner.Broadcasters;
using Blockcore.Features.Miner.Interfaces;
using Blockcore.Features.Miner.Staking;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet;
using Blockcore.Features.Wallet.UI;
using Blockcore.Interfaces.UI;
using Blockcore.Mining;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Networks.X1.Components
{
    /// <summary>
    /// Provides an ability to mine or stake.
    /// </summary>
    public class X1MiningFeature : FullNodeFeature
    {
        private readonly ConnectionManagerSettings connectionManagerSettings;

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        private readonly Network network;

        /// <summary>Settings relevant to mining or staking.</summary>
        private readonly X1MinerSettings minerSettings;

        /// <summary>Settings relevant to node.</summary>
        private readonly NodeSettings nodeSettings;

        /// <summary>POW miner.</summary>
        private readonly IPowMining powMining;

        /// <summary>POS staker.</summary>
        private readonly IPosMinting posMinting;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>State of time synchronization feature that stores collected data samples.</summary>
        private readonly ITimeSyncBehaviorState timeSyncBehaviorState;

        public X1MiningFeature(
            ConnectionManagerSettings connectionManagerSettings,
            Network network,
            MinerSettings minerSettings,
            NodeSettings nodeSettings,
            ILoggerFactory loggerFactory,
            ITimeSyncBehaviorState timeSyncBehaviorState,
            IPowMining powMining,
            IPosMinting posMinting = null)
        {
            this.connectionManagerSettings = connectionManagerSettings;
            this.network = network;
            this.minerSettings = (X1MinerSettings)minerSettings;
            this.nodeSettings = nodeSettings;
            this.powMining = powMining;
            this.timeSyncBehaviorState = timeSyncBehaviorState;
            this.posMinting = posMinting;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        /// <summary>
        /// Prints command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(Network network)
        {
            X1MinerSettings.PrintHelp(network);
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            X1MinerSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <summary>
        /// Starts staking a wallet.
        /// </summary>
        /// <param name="walletName">The name of the wallet.</param>
        /// <param name="walletPassword">The password of the wallet.</param>
        public void StartStaking(string walletName, string walletPassword)
        {
            if (this.timeSyncBehaviorState.IsSystemTimeOutOfSync)
            {
                string errorMessage = "Staking cannot start, your system time does not match that of other nodes on the network." + Environment.NewLine
                                    + "Please adjust your system time and restart the node.";
                this.logger.LogError(errorMessage);
                throw new ConfigurationException(errorMessage);
            }

            if (!string.IsNullOrEmpty(walletName) && !string.IsNullOrEmpty(walletPassword))
            {
                this.logger.LogInformation("Staking enabled on wallet '{0}'.", walletName);

                this.posMinting.Stake(new WalletSecret
                {
                    WalletPassword = walletPassword,
                    WalletName = walletName
                });
            }
            else
            {
                string errorMessage = "Staking not started, wallet name or password were not provided.";
                this.logger.LogError(errorMessage);
                throw new ConfigurationException(errorMessage);
            }
        }

        /// <summary>
        /// Stop a staking wallet.
        /// </summary>
        public void StopStaking()
        {
            this.posMinting?.StopStake();
            this.logger.LogInformation("Staking stopped.");
        }

        /// <summary>
        /// Stop a Proof of Work miner.
        /// </summary>
        public void StopMining()
        {
            this.powMining?.StopMining();
            this.logger.LogInformation("Mining stopped.");
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            if (this.minerSettings.Mine)
            {
                    this.powMining.Mine(GetMineToAddress(this.minerSettings.MineAddress));
                    this.logger.LogInformation("X1 Mining enabled.");
            }

            if (this.minerSettings.Stake)
            {
                this.StartStaking(this.minerSettings.WalletName, this.minerSettings.WalletPassword);
            }

            return Task.CompletedTask;
        }

        Script GetMineToAddress(string address)
        {
            try
            {
                byte[] bytes = this.network.Bech32Encoders.First().Decode(address, out byte witVersion);

                if (witVersion == 0 && bytes.Length == 20)
                    return new BitcoinWitPubKeyAddress(address, this.network).ScriptPubKey;
                if (witVersion == 0 && bytes.Length == 32)
                    return new BitcoinWitScriptAddress(address, this.network).ScriptPubKey;
                throw new InvalidOperationException("Invalid value for 'mineaddress' in .config or parameters.");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Mining is enabled but misconfigured.", e);
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            this.StopMining();
            this.StopStaking();
        }

        /// <inheritdoc />
        public override void ValidateDependencies(IFullNodeServiceProvider services)
        {
            if (services.ServiceProvider.GetService<IPosMinting>() != null)
            {
                services.Features.EnsureFeature<BaseWalletFeature>();
            }

            // Mining and staking require block store feature.
            if (this.minerSettings.Mine || this.minerSettings.Stake)
            {
                services.Features.EnsureFeature<BlockStoreFeature>();
                var storeSettings = services.ServiceProvider.GetService<StoreSettings>();
                if (storeSettings.PruningEnabled)
                    throw new ConfigurationException("BlockStore prune mode is incompatible with mining and staking.");
            }
        }
    }
}