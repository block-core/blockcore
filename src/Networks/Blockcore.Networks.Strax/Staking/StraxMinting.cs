using Blockcore.AsyncWork;
using Blockcore.Base;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.Consensus.Interfaces;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.Miner;
using Blockcore.Features.Miner.Staking;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Interfaces;
using Blockcore.Mining;
using Blockcore.Networks.Strax.Rules;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;


namespace Blockcore.Networks.Strax.Staking
{
    public class StraxMinting : PosMinting
    {
        public StraxMinting(
            IBlockProvider blockProvider,
            IConsensusManager consensusManager,
            ChainIndexer chainIndexer,
            Network network,
            IDateTimeProvider dateTimeProvider,
            IInitialBlockDownloadState initialBlockDownloadState,
            INodeLifetime nodeLifetime,
            ICoinView coinView,
            IStakeChain stakeChain,
            IStakeValidator stakeValidator,
            MempoolSchedulerLock mempoolLock,
            ITxMempool mempool,
            IWalletManager walletManager,
            IAsyncProvider asyncProvider,
            ITimeSyncBehaviorState timeSyncBehaviorState,
            ILoggerFactory loggerFactory,
            MinerSettings minerSettings) : base(blockProvider, consensusManager, chainIndexer, network, dateTimeProvider,
                initialBlockDownloadState, nodeLifetime, coinView, stakeChain, stakeValidator, mempoolLock, mempool,
                walletManager, asyncProvider, timeSyncBehaviorState, loggerFactory, minerSettings)
        {
        }

        public override Transaction PrepareCoinStakeTransactions(int currentChainHeight, CoinstakeContext coinstakeContext, long coinstakeOutputValue, int utxosCount, long amountStaked, long reward)
        {
            long cirrusReward = reward * StraxCoinviewRule.CirrusRewardPercentage / 100;

            coinstakeOutputValue -= cirrusReward;

            // Populate the initial coinstake with the modified overall reward amount, the outputs will be split as necessary
            base.PrepareCoinStakeTransactions(currentChainHeight, coinstakeContext, coinstakeOutputValue, utxosCount, amountStaked, reward);

            // Now add the remaining reward into an additional output on the coinstake
            var cirrusRewardOutput = new TxOut(cirrusReward, StraxCoinstakeRule.CirrusRewardScript);
            coinstakeContext.CoinstakeTx.Outputs.Add(cirrusRewardOutput);

            return coinstakeContext.CoinstakeTx;
        }
    }
}
