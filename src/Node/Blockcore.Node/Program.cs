using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Utilities;
 
namespace Blockcore.Node
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                string chain = args
                   .DefaultIfEmpty("--chain=BTC")
                   .Where(arg => arg.StartsWith("--chain", ignoreCase: true, CultureInfo.InvariantCulture))
                   .Select(arg => arg.Replace("--chain=", string.Empty, ignoreCase: true, CultureInfo.InvariantCulture))
                   .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(chain))
                {
                    chain = "BTC";
                }

                string version = args
                                   .Where(arg => arg.StartsWith("--upgradedbonversion", ignoreCase: true, CultureInfo.InvariantCulture))
                                   .Select(arg => arg.Replace("--upgradedbonversion=", string.Empty, ignoreCase: true, CultureInfo.InvariantCulture))
                                   .FirstOrDefault();

                if (version != null && version != "") {


                    var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                    var x42MainFolder = appDataFolder + "\\Blockcore\\x42\\x42Main";

                    VersionFileData versionFileData = new VersionFileData() { Version = version, Upgraded = false };
                    string fileName = x42MainFolder + "\\upgrade.json";

                    if (!File.Exists(fileName))
                    {

                        File.WriteAllText(fileName, JsonSerializer.Serialize(versionFileData));
                    }
                    else {

                        versionFileData = JsonSerializer.Deserialize<VersionFileData>(File.ReadAllText(fileName));
                  
                    }

                    if (versionFileData.Version == version && versionFileData.Upgraded == false)
                    {
                        var txDbData = appDataFolder + "\\Blockcore\\x42\\x42Main\\txdb";
                        if (Directory.Exists(txDbData))
                        {
                            Directory.Delete(txDbData, true);

                        }
                        versionFileData.Upgraded = true;
                        File.WriteAllText(fileName, JsonSerializer.Serialize(versionFileData));

                    }

                }




                NodeSettings nodeSettings = NetworkSelector.Create(chain, args);
                IFullNodeBuilder nodeBuilder = NodeBuilder.Create(chain, nodeSettings);

                IFullNode node = nodeBuilder.Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex);
            }
        }
    }
}
