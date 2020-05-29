using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace x42.Features.xServer
{
    /// <summary>
    /// This interface defines a xServer peers file used to cache the whitelisted peers discovered by the xServer feature service.
    /// </summary>
    public class xServerPeers
    {
        // <summary>
        /// The xServer path to peers file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The xServer peers file.
        /// </summary>
        private List<xServerPeer> Peers = new List<xServerPeer>();

        /// <summary>
        /// Protects access to the list of xServer Peers.
        /// </summary>
        private readonly object xServerPeersLock;

        public xServerPeers(string filePath)
        {
            this.xServerPeersLock = new object();

            this.Path = filePath;
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            }
            else if (File.Exists(filePath))
            {
                Load();
            }
        }

        /// <summary>
        /// Loads the saved xServer peers file from the specified stream.
        /// </summary>
        private void Load()
        {
            using (var peersFile = new StreamReader(this.Path))
            {
                string peersData = peersFile.ReadToEnd();
                lock (this.xServerPeersLock)
                {
                    this.Peers = JsonConvert.DeserializeObject<List<xServerPeer>>(peersData);
                }
            }
        }

        /// <summary>
        /// Get's a copy of the peers list.
        /// </summary>
        public List<xServerPeer> GetPeers()
        {
            lock (this.xServerPeersLock)
            {
                return this.Peers.ToList();
            }
        }

        /// <summary>
        /// Replaces peers list with list provided.
        /// </summary>
        public void ReplacePeers(List<xServerPeer> peers)
        {
            lock (this.xServerPeersLock)
            {
                this.Peers = peers;
            }
        }

        /// <summary>
        /// Saves the cached xServer peers file to the specified stream.
        /// </summary>
        public Task Save()
        {
            using (StreamWriter file = File.CreateText(this.Path))
            {
                var serializer = new JsonSerializer();
                lock (this.xServerPeersLock)
                {
                    serializer.Serialize(file, this.Peers);
                }
            }

            return Task.CompletedTask;
        }
    }
}