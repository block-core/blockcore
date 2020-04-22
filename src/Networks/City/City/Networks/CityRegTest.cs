using System;
using City.Networks.Policies;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace City.Networks
{
   public class CityRegTest : CityMain
   {
      public CityRegTest()
      {
         NetworkType = NetworkType.Regtest;

         Name = CitySetup.RegTest.Name;
         CoinTicker = CitySetup.RegTest.CoinTicker;
         Magic = ConversionTools.ConvertToUInt32(CitySetup.Magic, true);
         RootFolderName = CitySetup.RegTest.RootFolderName;
         DefaultPort = CitySetup.RegTest.DefaultPort;
         DefaultRPCPort = CitySetup.RegTest.DefaultRPCPort;
         DefaultAPIPort = CitySetup.RegTest.DefaultAPIPort;
         DefaultSignalRPort = CitySetup.RegTest.DefaultSignalRPort;

         var consensusFactory = new PosConsensusFactory();

         Block genesisBlock = CreateGenesisBlock(consensusFactory,
            CitySetup.RegTest.GenesisTime,
            CitySetup.RegTest.GenesisNonce,
            CitySetup.RegTest.GenesisBits,
            CitySetup.RegTest.GenesisVersion,
            CitySetup.RegTest.GenesisReward,
            CitySetup.GenesisText);

         Genesis = genesisBlock;

         // Taken from StratisX.
         var consensusOptions = new PosConsensusOptions(
             maxBlockBaseSize: 1_000_000,
             maxStandardVersion: 2,
             maxStandardTxWeight: 100_000,
             maxBlockSigopsCost: 20_000,
             maxStandardTxSigopsCost: 20_000 / 5,
             witnessScaleFactor: 4
         );

         var buriedDeployments = new BuriedDeploymentsArray
         {
            [BuriedDeployments.BIP34] = 0,
            [BuriedDeployments.BIP65] = 0,
            [BuriedDeployments.BIP66] = 0
         };

         Consensus = new NBitcoin.Consensus(
             consensusFactory: consensusFactory,
             consensusOptions: consensusOptions,
             coinType: CitySetup.CoinType,
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
             premineReward: Money.Coins(CitySetup.PremineReward),
             proofOfWorkReward: Money.Coins(CitySetup.PoWBlockReward),
             targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
             targetSpacing: CitySetup.TargetSpacing,
             powAllowMinDifficultyBlocks: true,
             posNoRetargeting: true,
             powNoRetargeting: true,
             powLimit: new Target(new uint256("0000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
             minimumChainWork: null,
             isProofOfStake: true,
             lastPowBlock: CitySetup.LastPowBlock,
             proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
             proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
             proofOfStakeReward: Money.Coins(CitySetup.PoSBlockReward),
             proofOfStakeTimestampMask: CitySetup.ProofOfStakeTimestampMask
         );

         Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (CitySetup.RegTest.PubKeyAddress) };
         Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (CitySetup.RegTest.ScriptAddress) };
         Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (239) };
         Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
         Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x35), (0x83), (0x94) };
         Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2b };
         Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 115 };

         Bech32Encoders = new Bech32Encoder[2];
         var encoder = new Bech32Encoder(CitySetup.RegTest.CoinTicker);
         Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
         Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

         Checkpoints = CitySetup.RegTest.Checkpoints;
         DNSSeeds = CitySetup.RegTest.DNS;
         SeedNodes = CitySetup.RegTest.Nodes;

         StandardScriptsRegistry = new CityStandardScriptsRegistry();

         // 64 below should be changed to TargetSpacingSeconds when we move that field.
         Assert(DefaultBanTimeSeconds <= Consensus.MaxReorgLength * 64 / 2);

         Assert(Consensus.HashGenesisBlock == uint256.Parse(CitySetup.RegTest.HashGenesisBlock));
         Assert(Genesis.Header.HashMerkleRoot == uint256.Parse(CitySetup.RegTest.HashMerkleRoot));

         RegisterRules(Consensus);
         RegisterMempoolRules(Consensus);
      }
   }
}
