using System;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Networks.Molie.Deployments;
using Blockcore.Networks.Molie.Policies;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace Blockcore.Networks.Molie
{
    public class MolieRegTest : MolieMain
    {
        public MolieRegTest()
        {
            this.NetworkType = NetworkType.Regtest;

            this.Name = MolieSetup.RegTest.Name;
            this.CoinTicker = MolieSetup.RegTest.CoinTicker;
            this.Magic = MolieSetup.RegTest.Magic;
            this.RootFolderName = MolieSetup.RegTest.RootFolderName;
            this.DefaultPort = MolieSetup.RegTest.DefaultPort;
            this.DefaultRPCPort = MolieSetup.RegTest.DefaultRPCPort;
            this.DefaultAPIPort = MolieSetup.RegTest.DefaultAPIPort;

            var consensusFactory = new PosConsensusFactory();

            Block genesisBlock = CreateGenesisBlock(consensusFactory,
               MolieSetup.RegTest.GenesisTime,
               MolieSetup.RegTest.GenesisNonce,
               MolieSetup.RegTest.GenesisBits,
               MolieSetup.RegTest.GenesisVersion,
               MolieSetup.RegTest.GenesisReward,
               MolieSetup.GenesisText);

            this.Genesis = genesisBlock;

            // Taken from StratisX.
            var consensusOptions = new PosConsensusOptions()
            {
                MaxBlockBaseSize = 1_000_000,
                MaxStandardVersion = 2,
                MaxStandardTxWeight = 100_000,
                MaxBlockSigopsCost = 20_000,
                MaxStandardTxSigopsCost = 20_000 / 5,
                WitnessScaleFactor = 4
            };

            var buriedDeployments = new BuriedDeploymentsArray
            {
                [BuriedDeployments.BIP34] = 0,
                [BuriedDeployments.BIP65] = 0,
                [BuriedDeployments.BIP66] = 0
            };


            var bip9Deployments = new MolieBIP9Deployments()
            {
                // Always active.
                [MolieBIP9Deployments.CSV] = new BIP9DeploymentsParameters("CSV", 0, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultRegTestThreshold),
                [MolieBIP9Deployments.Segwit] = new BIP9DeploymentsParameters("Segwit", 1, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultRegTestThreshold),
                [MolieBIP9Deployments.ColdStaking] = new BIP9DeploymentsParameters("ColdStaking", 2, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultRegTestThreshold)
            };

            this.Consensus = new Consensus.Consensus(
             consensusFactory: consensusFactory,
             consensusOptions: consensusOptions,
             coinType: 1,
             hashGenesisBlock: genesisBlock.GetHash(),
             subsidyHalvingInterval: 210000,
             majorityEnforceBlockUpgrade: 750,
             majorityRejectBlockOutdated: 950,
             majorityWindow: 1000,
             buriedDeployments: buriedDeployments,
             bip9Deployments: bip9Deployments,
             bip34Hash: null,
             minerConfirmationWindow: 2016, // nPowTargetTimespan / nPowTargetSpacing
             maxReorgLength: 500,
             defaultAssumeValid: null,
             maxMoney: Money.Coins(MolieSetup.MaxSupply),
             coinbaseMaturity: 10,
             premineHeight: 2,
             premineReward: Money.Coins(MolieSetup.PremineReward),
             proofOfWorkReward: Money.Coins(MolieSetup.PoWBlockReward),
             targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
             targetSpacing: MolieSetup.TargetSpacing,
             powAllowMinDifficultyBlocks: true,
             posNoRetargeting: true,
             powNoRetargeting: true,
             powLimit: new Target(new uint256("0000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
             minimumChainWork: null,
             isProofOfStake: true,
             lastPowBlock: MolieSetup.RegTest.LastPowBlock,
             proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
             proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
             proofOfStakeReward: Money.Coins(MolieSetup.PoSBlockReward),
             proofOfStakeTimestampMask: MolieSetup.ProofOfStakeTimestampMask
         );

            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { MolieSetup.RegTest.PubKeyAddress };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { MolieSetup.RegTest.ScriptAddress };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { MolieSetup.RegTest.SecretAddress };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { 0x04, 0x35, 0x87, 0xCF };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { 0x04, 0x35, 0x83, 0x94 };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 115 };

            this.Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder(MolieSetup.RegTest.CoinTicker.ToLowerInvariant());
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.Checkpoints = MolieSetup.RegTest.Checkpoints;
            this.DNSSeeds = MolieSetup.RegTest.DNS;
            this.SeedNodes = MolieSetup.RegTest.Nodes;

            this.StandardScriptsRegistry = new MolieStandardScriptsRegistry();

            // 64 below should be changed to TargetSpacingSeconds when we move that field.
            Assert(this.DefaultBanTimeSeconds <= this.Consensus.MaxReorgLength * 64 / 2);

            Assert(this.Consensus.HashGenesisBlock == uint256.Parse(MolieSetup.RegTest.HashGenesisBlock));
            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse(MolieSetup.RegTest.HashMerkleRoot));

            RegisterRules(this.Consensus);
            RegisterMempoolRules(this.Consensus);
        }
    }
}
