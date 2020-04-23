using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Blockcore.Tests.Setup
{
    /// <summary>
    /// Tests related to the Blockcore Template and setup configuration for Blockcore Reference Nodes.
    /// </summary>
    public class SetupTests
    {
        [Fact]
        public void VerifyMagicFromHex()
        {
            uint bitcoinMagicOriginal = 0xD9B4BEF9;
            string bitcoinMagicText = "F9-BE-B4-D9";

            var bitcoinMagicArray = new byte[4];
            bitcoinMagicArray[0] = 0xF9;
            bitcoinMagicArray[1] = 0xBE;
            bitcoinMagicArray[2] = 0xB4;
            bitcoinMagicArray[3] = 0xD9;

            Assert.Equal(bitcoinMagicOriginal, BitConverter.ToUInt32(bitcoinMagicArray, 0));
            Assert.Equal(bitcoinMagicText, BitConverter.ToString(bitcoinMagicArray));
            Assert.Equal(bitcoinMagicArray, ConvertToByteArray(bitcoinMagicText));
            Assert.Equal(bitcoinMagicOriginal, ConvertToUInt32(bitcoinMagicText));

            uint blockcoreMagicOriginal = 0x424C4B02;
            string blockcoreMagicText = "02-4B-4C-42";

            var blockcoreMagicArray = new byte[4];
            blockcoreMagicArray[0] = 0x02;
            blockcoreMagicArray[1] = 0x4B;
            blockcoreMagicArray[2] = 0x4C;
            blockcoreMagicArray[3] = 0x42;

            Assert.Equal(blockcoreMagicOriginal, BitConverter.ToUInt32(blockcoreMagicArray, 0));
            Assert.Equal(blockcoreMagicText, BitConverter.ToString(blockcoreMagicArray));
            Assert.Equal(blockcoreMagicArray, ConvertToByteArray(blockcoreMagicText));
            Assert.Equal(blockcoreMagicOriginal, ConvertToUInt32(blockcoreMagicText));


            uint stratisMagicOriginal = 0x5223570;
            string stratisMagicText = "70-35-22-05";

            var stratisMagicArray = new byte[4];
            stratisMagicArray[0] = 0x70;
            stratisMagicArray[1] = 0x35;
            stratisMagicArray[2] = 0x22;
            stratisMagicArray[3] = 0x05;

            Assert.Equal(stratisMagicOriginal, BitConverter.ToUInt32(stratisMagicArray, 0));
            Assert.Equal(stratisMagicText, BitConverter.ToString(stratisMagicArray));
            Assert.Equal(stratisMagicArray, ConvertToByteArray(stratisMagicText));
            Assert.Equal(stratisMagicOriginal, ConvertToUInt32(stratisMagicText));


            uint cityMagicOriginal = 0x43545901;
            string cityMagicText = "01-59-54-43";

            var cityMagicArray = new byte[4];
            cityMagicArray[0] = 0x01;
            cityMagicArray[1] = 0x59;
            cityMagicArray[2] = 0x54;
            cityMagicArray[3] = 0x43;

            Assert.Equal(cityMagicOriginal, BitConverter.ToUInt32(cityMagicArray, 0));
            Assert.Equal(cityMagicText, BitConverter.ToString(cityMagicArray));
            Assert.Equal(cityMagicArray, ConvertToByteArray(cityMagicText));
            Assert.Equal(cityMagicOriginal, ConvertToUInt32(cityMagicText));
        }

        public static byte[] ConvertToByteArray(string magicText)
        {
            return magicText.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();
        }

        public static uint ConvertToUInt32(string magicText)
        {
            byte[] number = magicText.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();
            return BitConverter.ToUInt32(number);
        }

        /// <summary>
        /// Use this to get the text value from an original magic value.
        /// </summary>
        /// <param name="magic"></param>
        public static string ConvertToString(uint magic)
        {
            byte[] bytes = BitConverter.GetBytes(magic);
            return BitConverter.ToString(bytes);
        }
    }
}
