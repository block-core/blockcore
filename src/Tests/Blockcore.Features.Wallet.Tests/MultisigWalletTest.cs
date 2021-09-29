using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Features.Wallet.Helpers;
using Blockcore.Features.Wallet.Types;
using Blockcore.Networks;
using Blockcore.Networks.XRC;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Wallet.Tests
{
    public class MultisigWalletTest
    {
        private readonly Network Network;
        private readonly MultisigScheme multisigScheme;
        private const int COINTYPE = 10291;
        public MultisigWalletTest()
        {
            this.Network = new XRCMain();
            this.multisigScheme = new MultisigScheme()
            {
                Threashold = 2,
                XPubs = new string[]
                {
                    "xpub661MyMwAqRbcFK4g9bHwLNYLmy4JxSFkKNRURL3hJqVYLoZ318eHmEKjMbFxbgDiaMycDk4oixtRkHKgbopRzukzimmyzAb2aEVfAEeJPxt",
                    "xpub661MyMwAqRbcFVViGz3WGiaYgzsKWKXYduJZ8oR4tnnaRLp2QXMUf5P9Yq6CpJ8zuddutUFyrTkMECqqa1UyjnegVpoiYbcqpJfSCP9GcmG",
                    "xpub661MyMwAqRbcFYCYnw23jLqPAHMvCfmLeKkmDSLGy6nGpVtJvN9EDoR2qdz8fmvkV8sSa1ZT7j8oyfBgjHKX2nGnESLqSndrM2gJ6TGcXrU",
                }
            };
        }

        [Fact]
        public void pubKeysDerivedFromExtendedPrivateAndPublicKeysMatch() {
          
            string password = "bdemq1XLhLYbiGHD";
            string passphrase = password;

            string mnemonic = "chalk call anger chase endless level slow sleep coast left sand enter save bind curious puzzle stadium volume mixture shuffle hurry gas borrow believe";

            ExtKey extendedKey = HdOperations.GetExtendedKey(mnemonic, passphrase);

            string encryptedSeed = extendedKey.PrivateKey.GetEncryptedBitcoinSecret(password, this.Network).ToWif();
            Key privateKey = HdOperations.DecryptSeed(encryptedSeed, password, this.Network);

            string accountHdPath = HdOperations.GetAccountHdPath(COINTYPE, 0);
            string path = HdOperations.CreateHdPath(COINTYPE, 0, false, 0);

            ExtPubKey accountExtPubKey = HdOperations.GetExtendedPublicKey(privateKey, extendedKey.ChainCode, accountHdPath);
            var subjectPubKey = HdOperations.GeneratePublicKey(accountExtPubKey.ToString(this.Network), 0, false, this.Network);

            var subjectPrivKey = HdOperations.GetExtendedPrivateKey(privateKey, extendedKey.ChainCode, path, this.Network);

            Assert.Equal(subjectPubKey.ScriptPubKey, subjectPrivKey.PrivateKey.PubKey.ScriptPubKey);
        }        

        [Fact]
        public void Generate1stMultisigAddress()
        {

            WalletMultisig wallet = new WalletMultisig(this.Network);
            var root = new AccountRootMultisig
            {
                CoinType = COINTYPE
            };
            wallet.AccountsRoot.Add(root);
            var account = wallet.AddNewAccount(this.multisigScheme, COINTYPE, DateTimeOffset.UtcNow);

            Script redeemScript = account.GeneratePublicKey(0, this.Network);
            Assert.Equal("rbAxG3vTMCuVMWppaobvTajBtUHSiFtkr5", redeemScript.Hash.GetAddress(this.Network).ToString());
        }

        [Fact]
        public void Generate4SequnetialMultisigRecievingAndChangeAddress()
        {
            string[] receiving = { "rbAxG3vTMCuVMWppaobvTajBtUHSiFtkr5", "roLKXofxDFrkZqW7kU9aD7j7E6pTajEe16", "rjmcR79w6MatNK3KeHR3FvaEEHngdAKa9h", "reD6MyXPFUaJpyBtJVDgdYf7iN4qHbhzBz" };
            string[] change = { "rXh3PVYpn462fDTAmLiUyKZ1aTWdzr4W9J", "rjNVcwuXKW1d8j8CQbxK4Kim5Hkg7Z4ZhL", "rginUKQEG9XjQVfhJ5ho7P4v9ENrtvF8Uk", "rgeVtHemGFGQzRU2Dhj3gYTgsWcjg4wgG5" };

            WalletMultisig wallet = new WalletMultisig(this.Network);
            var root = new AccountRootMultisig
            {
                CoinType = COINTYPE
            };
            wallet.AccountsRoot.Add(root);
            var account = wallet.AddNewAccount(this.multisigScheme, COINTYPE, DateTimeOffset.UtcNow);

            for (int i = 0; i < receiving.Length; i++)
            {
                Script redeemScript = account.GeneratePublicKey(i, this.Network);
                Assert.Equal(receiving[i], redeemScript.Hash.GetAddress(this.Network).ToString());
            }

            for (int i = 0; i < change.Length; i++)
            {
                Script redeemScript = account.GeneratePublicKey(i, this.Network, true);
                Assert.Equal(change[i], redeemScript.Hash.GetAddress(this.Network).ToString());
            }
        }
    }
}
