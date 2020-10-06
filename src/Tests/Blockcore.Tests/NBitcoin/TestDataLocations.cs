using System.IO;

namespace NBitcoin.Tests
{
    public class TestDataLocations
    {
        public static string GetFileFromDataFolder(string filename)
        {
            return Path.Combine("NBitcoin", "data", filename);
        }

        public static string GetFileFromDataBlockFolder(string filename)
        {
            return Path.Combine("NBitcoin", "data", "blocks", filename);
        }

        public static string GetFileFromDataPosFolder(string filename)
        {
            return Path.Combine("NBitcoin", "data_pos", filename);
        }

        public static string GetFileFromDataPosBlockFolder(string filename)
        {
            return Path.Combine("NBitcoin", "data_pos", "blocks", filename);
        }
    }
}