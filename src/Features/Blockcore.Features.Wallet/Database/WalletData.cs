using System.Collections.Generic;
using Blockcore.Utilities;
using LiteDB;
using NBitcoin;

namespace Blockcore.Features.Wallet.Database
{
    public class WalletData
    {
        [BsonId]
        public string Key { get; set; }

        public string EncryptedSeed { get; set; }

        public string WalletName { get; set; }

        public HashHeightPair WalletTip { get; set; }

        public ICollection<uint256> BlockLocator { get; set; }
    }
}