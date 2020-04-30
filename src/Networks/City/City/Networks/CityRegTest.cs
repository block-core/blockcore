using System;
using System.Linq;
using System.Net;
using City.Networks.Consensus;
using City.Networks.Policies;
using City.Networks.Setup;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace City.Networks
{
   public class CityRegTest : CityMain
   {
      public CityRegTest()
      {
         // START MODIFICATIONS OF GENERATED CODE
         var consensusOptions = new CityPosConsensusOptions(
             maxBlockBaseSize: 1_000_000,
             maxStandardVersion: 2,
             maxStandardTxWeight: 100_000,
             maxBlockSigopsCost: 20_000,
             maxStandardTxSigopsCost: 20_000 / 5,
             witnessScaleFactor: 4
         );
         // END MODIFICATIONS

         CoinSetup setup = CitySetup.Instance.Setup;
         NetworkSetup network = CitySetup.Instance.RegTest;

         NetworkType = NetworkType.Regtest;

         Name = network.Name;
         CoinTicker = network.CoinTicker;
         Magic = ConversionTools.ConvertToUInt32(setup.Magic, true);
         RootFolderName = network.RootFolderName;
         DefaultPort = network.DefaultPort;
         DefaultRPCPort = network.DefaultRPCPort;
         DefaultAPIPort = network.DefaultAPIPort;
         DefaultSignalRPort = network.DefaultSignalRPort;

         var consensusFactory = new PosConsensusFactory();

         // Create the genesis block.
         GenesisTime = network.GenesisTime;
         GenesisNonce = network.GenesisNonce;
         GenesisBits = network.GenesisBits;
         GenesisVersion = network.GenesisVersion;
         GenesisReward = network.GenesisReward;

         Block genesisBlock = CreateGenesisBlock(consensusFactory,
            GenesisTime,
            GenesisNonce,
            GenesisBits,
            GenesisVersion,
            GenesisReward,
            setup.GenesisText);

         Genesis = genesisBlock;

         var buriedDeployments = new BuriedDeploymentsArray
         {
            [BuriedDeployments.BIP34] = 0,
            [BuriedDeployments.BIP65] = 0,
            [BuriedDeployments.BIP66] = 0
         };

         Consensus = new NBitcoin.Consensus(
             consensusFactory: consensusFactory,
             consensusOptions: consensusOptions,
             coinType: setup.CoinType,
             hashGenesisBlock: genesisBlock.GetHash(),
             subsidyHalvingInterval: 24333,
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
             powAllowMinDifficultyBlocks: true,
             posNoRetargeting: true,
             powNoRetargeting: true,
             powLimit: new Target(new uint256("0000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
             minimumChainWork: null,
             isProofOfStake: true,
             lastPowBlock: setup.LastPowBlock,
             proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
             proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
             proofOfStakeReward: Money.Coins(setup.PoSBlockReward),
             proofOfStakeTimestampMask: setup.ProofOfStakeTimestampMask
         );

         Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (byte)network.PubKeyAddress };
         Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (byte)network.ScriptAddress };
         Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (239) };
         Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
         Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x35), (0x83), (0x94) };
         Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2b };
         Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 115 };

         Bech32Encoders = new Bech32Encoder[2];
         var encoder = new Bech32Encoder(network.CoinTicker);
         Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
         Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

         Checkpoints = network.Checkpoints;
         DNSSeeds = network.DNS.Select(dns => new DNSSeedData(dns, dns)).ToList();
         SeedNodes = network.Nodes.Select(node => new NBitcoin.Protocol.NetworkAddress(IPAddress.Parse(node), network.DefaultPort)).ToList();

         StandardScriptsRegistry = new CityStandardScriptsRegistry();

         // 64 below should be changed to TargetSpacingSeconds when we move that field.
         Assert(DefaultBanTimeSeconds <= Consensus.MaxReorgLength * 64 / 2);

         Assert(Consensus.HashGenesisBlock == uint256.Parse(network.HashGenesisBlock));
         Assert(Genesis.Header.HashMerkleRoot == uint256.Parse(network.HashMerkleRoot));

         RegisterRules(Consensus);
         RegisterMempoolRules(Consensus);
      }
   }
}
