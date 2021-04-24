using System;
using System.Linq;
using System.Net;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Networks;
using Blockcore.P2P;
using Blockcore.Networks.SERF.Policies;
using Blockcore.Networks.SERF.Setup;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace Blockcore.Networks.SERF
{
   public class SERFTest : SERFMain
   {
      public SERFTest()
      {
         CoinSetup setup = SERFSetup.Instance.Setup;
         NetworkSetup network = SERFSetup.Instance.Test;

            this.NetworkType = NetworkType.Testnet;

            this.Name = network.Name;
            this.CoinTicker = network.CoinTicker;
            this.Magic = ConversionTools.ConvertToUInt32(setup.Magic, true);
            this.RootFolderName = network.RootFolderName;
            this.DefaultPort = network.DefaultPort;
            this.DefaultRPCPort = network.DefaultRPCPort;
            this.DefaultAPIPort = network.DefaultAPIPort;

         var consensusFactory = new PosConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = network.GenesisTime;
            this.GenesisNonce = network.GenesisNonce;
            this.GenesisBits = network.GenesisBits;
            this.GenesisVersion = network.GenesisVersion;
            this.GenesisReward = network.GenesisReward;

         Block genesisBlock = CreateGenesisBlock(consensusFactory,
            this.GenesisTime,
            this.GenesisNonce,
            this.GenesisBits,
            this.GenesisVersion,
            this.GenesisReward,
            setup.GenesisText);

            this.Genesis = genesisBlock;

         var consensusOptions = new PosConsensusOptions
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

            this.Consensus = new Blockcore.Consensus.Consensus(
             consensusFactory: consensusFactory,
             consensusOptions: consensusOptions,
             coinType: setup.CoinType,
             hashGenesisBlock: genesisBlock.GetHash(),
             subsidyHalvingInterval: 210000,
             majorityEnforceBlockUpgrade: 750,
             majorityRejectBlockOutdated: 950,
             majorityWindow: 1000,
             buriedDeployments: buriedDeployments,
             bip9Deployments: new NoBIP9Deployments(),
             bip34Hash: null,
             minerConfirmationWindow: 2016, // nPowTargetTimespan / nPowTargetSpacing
             maxReorgLength: 500,
             defaultAssumeValid: null,
             maxMoney: long.MaxValue,
             coinbaseMaturity: 10,
             premineHeight: 2,
             premineReward: Money.Coins(setup.PremineReward),
             proofOfWorkReward: Money.Coins(setup.PoWBlockReward),
             targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
             targetSpacing: setup.TargetSpacing,
             powAllowMinDifficultyBlocks: false,
             posNoRetargeting: false,
             powNoRetargeting: false,
             powLimit: new Target(new uint256("000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
             minimumChainWork: null,
             isProofOfStake: true,
             lastPowBlock: setup.LastPowBlock,
             proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
             proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
             proofOfStakeReward: Money.Coins(setup.PoSBlockReward),
             proofOfStakeTimestampMask: setup.ProofOfStakeTimestampMask
         );

            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (byte)network.PubKeyAddress };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (byte)network.ScriptAddress };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (239) };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            this.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            this.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 115 };

            this.Bech32Encoders = new Bech32Encoder[2];
         var encoder = new Bech32Encoder(network.CoinTicker.ToLowerInvariant());
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.Checkpoints = network.Checkpoints;
            this.DNSSeeds = network.DNS.Select(dns => new DNSSeedData(dns, dns)).ToList();
            this.SeedNodes = network.Nodes.Select(node => new NBitcoin.Protocol.NetworkAddress(IPAddress.Parse(node), network.DefaultPort)).ToList();

            this.StandardScriptsRegistry = new SERFStandardScriptsRegistry();

         // 64 below should be changed to TargetSpacingSeconds when we move that field.
         Assert(this.DefaultBanTimeSeconds <= this.Consensus.MaxReorgLength * 64 / 2);

         Assert(this.Consensus.HashGenesisBlock == uint256.Parse(network.HashGenesisBlock));
         Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse(network.HashMerkleRoot));

         RegisterRules(this.Consensus);
         RegisterMempoolRules(this.Consensus);
      }
   }
}
