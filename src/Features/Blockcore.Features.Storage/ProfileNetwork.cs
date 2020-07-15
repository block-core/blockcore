using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace Blockcore.Features.Storage
{
    public class ProfileNetwork : Network
    {
        private static ProfileNetwork network;

        public static ProfileNetwork Instance
        {
            get
            {

                if (network == null)
                {
                    network = new ProfileNetwork();
                }

                return network;
            }
        }

        public ProfileNetwork()
        {
            Name = "PROFILE";
            CoinTicker = "ID";

            Base58Prefixes = new byte[12][];
            Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { 55 };
            Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { 117 };
            Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { 55 + 128 };

            //Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            //Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            //Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            //Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            //Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            //Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            //Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2a };
            //Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };
            //Base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };
        }
    }
}
