using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Blockcore.Features.Storage.Tests
{
    public class NetworkSyncTests
    {
        private readonly ITestOutputHelper output;

        public NetworkSyncTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void SimulateNewDocumentsSyncInFullNetwork()
        {
            var nodeA = new Node { IP = "1", Log = this.output };
            var nodeB = new Node { IP = "2", Log = this.output };
            var nodeC = new Node { IP = "3", Log = this.output };
            var nodeD = new Node { IP = "4", Log = this.output };
            var nodeE = new Node { IP = "5", Log = this.output };
            var nodeF = new Node { IP = "6", Log = this.output };
            var nodeG = new Node { IP = "7", Log = this.output };
            var nodeH = new Node { IP = "8", Log = this.output };

            List<Node> nodes = new List<Node>();
            nodes.Add(nodeA);
            nodes.Add(nodeB);
            nodes.Add(nodeC);
            nodes.Add(nodeD);
            nodes.Add(nodeE);
            nodes.Add(nodeF);
            nodes.Add(nodeG);
            nodes.Add(nodeH);

            // B connects to A.
            nodeA.OnConnect(nodeB);

            // B connects to C.
            nodeC.OnConnect(nodeB);

            // Simulate that nodeA receives new document from user.
            nodeA.Add(new Document { Id = "1", Name = "My first Identity" });
            // After the above add, nodeB should receive the full document immediately, and it should forward
            // the new ID to nodeC, which will then request that document from nodeB.

            // Verify that everyone has the document.
            Assert.Equal(1, nodeA.Documents.Count);
            Assert.Equal(1, nodeB.Documents.Count);
            Assert.Equal(1, nodeC.Documents.Count);

            nodeA.OnConnect(nodeD); /// nodeD.OnConnect(nodeA) doesn't work.

            Assert.Equal(1, nodeD.Documents.Count);

            nodeA.Add(new Document { Id = "2", Name = "Second Identity" });

            Assert.Equal(2, nodeA.Documents.Count);
            Assert.Equal(2, nodeB.DocumentsReceived);
            Assert.Equal(2, nodeB.Documents.Count);
            Assert.Equal(2, nodeC.Documents.Count);
            Assert.Equal(2, nodeD.Documents.Count);

            nodeD.OnConnect(nodeE);
            nodeD.OnConnect(nodeF);
            nodeD.OnConnect(nodeG);
            nodeD.OnConnect(nodeH);

            // Retest these
            Assert.Equal(2, nodeA.Documents.Count);
            Assert.Equal(2, nodeB.DocumentsReceived);
            Assert.Equal(2, nodeC.Documents.Count);
            Assert.Equal(2, nodeD.Documents.Count);

            Assert.Equal(2, nodeE.Documents.Count);
            Assert.Equal(2, nodeF.Documents.Count);
            Assert.Equal(2, nodeG.Documents.Count);
            Assert.Equal(2, nodeH.Documents.Count);

            nodeA.Add(new Document { Id = "3", Name = "Third Identity" });

            // Retest these
            Assert.Equal(3, nodeA.Documents.Count);
            Assert.Equal(3, nodeB.DocumentsReceived);
            Assert.Equal(3, nodeB.Documents.Count);
            Assert.Equal(3, nodeC.Documents.Count);
            Assert.Equal(3, nodeD.Documents.Count);
            Assert.Equal(3, nodeE.Documents.Count);
            Assert.Equal(3, nodeF.Documents.Count);
            Assert.Equal(3, nodeG.Documents.Count);
            Assert.Equal(3, nodeH.Documents.Count);

            nodeH.OnConnect(nodeA);

            // Retest these
            Assert.Equal(3, nodeA.Documents.Count);
            Assert.Equal(3, nodeB.DocumentsReceived);
            Assert.Equal(3, nodeB.Documents.Count);
            Assert.Equal(3, nodeC.Documents.Count);
            Assert.Equal(3, nodeD.Documents.Count);
            Assert.Equal(3, nodeE.Documents.Count);
            Assert.Equal(3, nodeF.Documents.Count);
            Assert.Equal(3, nodeG.Documents.Count);
            Assert.Equal(3, nodeH.Documents.Count);

            nodeA.Add(new Document { Id = "3", Name = "UPDATED Third Identity" });

            Assert.Equal("UPDATED Third Identity", nodeH.Documents.SingleOrDefault(d => d.Id == "3").Name);
        }

        [Fact]
        public void SimulateExistingDocumentsSyncInFullNetwork()
        {
            var nodeA = new Node { IP = "1", Log = this.output };
            var nodeB = new Node { IP = "2", Log = this.output };
            var nodeC = new Node { IP = "3", Log = this.output };
            var nodeD = new Node { IP = "4", Log = this.output };

            List<Node> nodes = new List<Node>();
            nodes.Add(nodeA);
            nodes.Add(nodeB);
            nodes.Add(nodeC);
            nodes.Add(nodeD);

            nodeD.Documents.Add(new Document { Id = "1", Name = "Number Uno" });
            nodeD.Ids.Add("1");

            nodeD.Documents.Add(new Document { Id = "2", Name = "Number Two" });
            nodeD.Ids.Add("2");

            nodeD.Documents.Add(new Document { Id = "3", Name = "Number Three" });
            nodeD.Ids.Add("3");

            nodeA.OnConnect(nodeB);
            nodeA.OnConnect(nodeC);

            // NodeD is offline and holds 3 documents, while the others are empty.
            Assert.Equal(0, nodeA.Documents.Count);
            Assert.Equal(0, nodeB.Documents.Count);
            Assert.Equal(0, nodeC.Documents.Count);
            Assert.Equal(3, nodeD.Documents.Count);

            // Connect to NodeD which has data and watch it flow into NodeA.
            nodeA.OnConnect(nodeD);

            // The B node and node C have not yet received data, cause A node "asked for it", it won't relay what it received.
            Assert.Equal(3, nodeA.Documents.Count);
            Assert.Equal(0, nodeB.Documents.Count);
            Assert.Equal(0, nodeC.Documents.Count);
            Assert.Equal(3, nodeD.Documents.Count);

            // Make sure that nodeB connects to D and get's data.
            nodeB.OnConnect(nodeD);

            Assert.Equal(3, nodeA.Documents.Count);
            Assert.Equal(3, nodeB.Documents.Count);
            Assert.Equal(0, nodeC.Documents.Count);
            Assert.Equal(3, nodeD.Documents.Count);

            // Updated document is observed on the network, everyone should get this.
            nodeA.Add(new Document { Id = "3", Name = "Number Three!" });

            Assert.Equal(3, nodeA.Documents.Count);
            Assert.Equal(3, nodeB.Documents.Count);
            Assert.Equal(1, nodeC.Documents.Count); // Observed only the new document.
            Assert.Equal(3, nodeD.Documents.Count);

            Assert.Equal("Number Three!", nodeC.Documents[0].Name);
            Assert.Equal("Number Three!", nodeD.Documents.SingleOrDefault(d => d.Id == "3").Name);

            // How can we make nodeC get what it is missing?
            // We must connect it to another node, which at this moment in time have received a valid sync.
            nodeC.OnConnect(nodeB);

            Assert.Equal(3, nodeA.Documents.Count);
            Assert.Equal(3, nodeB.Documents.Count);
            Assert.Equal(3, nodeC.Documents.Count);
            Assert.Equal(3, nodeD.Documents.Count);
        }
    }

    public class Document
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }

    public class Node
    {
        public ITestOutputHelper Log { get; set; }

        public string IP { get; set; }

        public List<string> Ids { get; set; } = new List<string>();

        public List<Document> Documents { get; set; } = new List<Document>();

        public int DocumentsReceived { get; set; }

        public List<Node> Peers { get; set; } = new List<Node>();

        public void OnConnect(Node node)
        {
            // Add connection both ways.
            this.Peers.Add(node);
            node.Peers.Add(this);

            Log.WriteLine($"IP{this.IP} connected to IP{node.IP}.");

            // Announce to incoming node what we have of data.
            this.SendMyIds(node);

            // The other node receives the same OnConnect event so let us simulate SendMyIds the other way.
            node.SendMyIds(this);
        }

        public void Add(Document document)
        {
            var index = this.Documents.FindIndex(d => d.Id == document.Id);

            if (index > -1)
            {
                this.Documents[index] = document;
                Log.WriteLine($"IP{this.IP}: Observed updated document. Sending to all peers: {string.Join(',', this.Peers.Select(p => p.IP))}.");
            }
            else
            {
                this.Documents.Add(document);
                this.Ids.Add(document.Id);
                Log.WriteLine($"IP{this.IP}: Observed new document. Sending to all peers: {string.Join(',', this.Peers.Select(p => p.IP))}.");
            }

            // The document was added through API, so we can distribute the whole thing immediately.
            foreach (Node peer in this.Peers)
            {
                this.SendDocuments(new string[1] { document.Id }, peer, false);
            }
        }

        public void SendMyIds(Node peer)
        {
            // Send all ids to the peer.
            peer.OnReceivedIds(this.Ids, this);
        }

        public void DoYouWantThis(IEnumerable<string> ids, Node peer)
        {
            string[] idsArray = ids.ToArray();

            // Find the unique ids of the incoming peer then ask for the data.
            string[] yourUniqueIds = idsArray.Except(this.Ids).ToArray();

            // We just received a bunch of ids from the peer.
            Log.WriteLine($"IP{this.IP}: Received {idsArray.Length} IDs from IP{peer.IP}.");

            if (yourUniqueIds.Length > 0)
            {
                Log.WriteLine($"IP{this.IP}: Asking for missing documents: {string.Join(',', yourUniqueIds)} from IP{peer.IP}.");

                // Give me the data I'm missing.
                peer.GiveMeDocuments(yourUniqueIds, this);
            }
        }

        public void OnReceivedIds(IEnumerable<string> ids, Node peer)
        {
            string[] idsArray = ids.ToArray();

            // Find the unique ids of the incoming peer then ask for the data.
            string[] yourUniqueIds = idsArray.Except(this.Ids).ToArray();

            // We just received a bunch of ids from the peer.
            Log.WriteLine($"IP{this.IP}: Received {idsArray.Length} IDs from IP{peer.IP}.");

            if (yourUniqueIds.Length > 0)
            {
                Log.WriteLine($"IP{this.IP}: Asking for missing documents: {string.Join(',', yourUniqueIds)} from IP{peer.IP}.");

                // Give me the data I'm missing.
                peer.GiveMeDocuments(yourUniqueIds, this);
            }

            // Here are my unique ids, do you want them?
            string[] myUniqueIds = this.Ids.Except(idsArray).ToArray();

            if (myUniqueIds.Length > 0)
            {
                Log.WriteLine($"IP{this.IP}: Tell what I have that you don't have: {string.Join(',', myUniqueIds)}. To IP{peer.IP}.");

                peer.WhatIHave(myUniqueIds, this);
            }
        }

        public void WhatIHave(IEnumerable<string> ids, Node peer)
        {
            // We just received a response back from initial diff-check, should we trust that we did not receive anything from anything
            // else while this went on, or should we do our own filtering?

            // We do our own filter again to see if we actually need something.
            var idsImStillMissing = ids.Except(this.Ids).ToArray();

            // Ask the peer to get those missing documents.
            peer.GiveMeDocuments(idsImStillMissing, this);
        }

        public void GiveMeDocuments(IEnumerable<string> ids, Node peer)
        {
            IEnumerable<Document> documentToSendBack = this.Documents.Where(d => ids.Contains(d.Id));
            peer.OnReceivedDocuments(documentToSendBack, this, true);
        }

        public void OnReceivedDocuments(IEnumerable<Document> documents, Node peer, bool askedForIt)
        {
            // We received incoming documents, add to our state.
            var ids = documents.Select(d => d.Id).ToArray();
            this.DocumentsReceived += ids.Length;

            Log.WriteLine($"IP{this.IP}: Received {ids.Length} documents from IP{peer.IP}. Asked for it: {askedForIt}.");

            // Filter only new documents so we don't insert again.
            foreach (var document in documents)
            {
                var index = this.Documents.FindIndex(d => d.Id == document.Id);

                if (index > -1)
                {
                    this.Documents[index] = document;
                }
                else
                {
                    this.Documents.Add(document);
                    this.Ids.Add(document.Id);
                }
            }

            // If we didn't ask specifically for these documents, we'll broadcast to our peers what we just received randomly from another peer.
            if (!askedForIt)
            {
                // After we received a new document, let us inform our peers about the IDs we just got.
                foreach (var p in this.Peers)
                {
                    // Skip the peer.
                    if (p.IP == peer.IP)
                    {
                        continue;
                    }

                    // This peer just received a new document from another that we didn't ask for, so
                    // we'll just forward the ID and notify our peers that "hey, we got a new document, do you want a copy?"
                    p.DoYouWantThis(ids, this);
                }
            }
        }

        public void SendIds(IEnumerable<string> ids, Node peer)
        {
            IEnumerable<string> myUniqueIds = this.Ids.Except(ids);
            IEnumerable<string> yourUniqueIds = ids.Except(this.Ids);

            // Give me these documents!
            peer.SendDocuments(yourUniqueIds, this, true);

            // Here, I have these extra
            peer.SendIds(myUniqueIds, this);
        }

        public void SendDocuments(IEnumerable<string> ids, Node peer, bool askedForIt)
        {
            // Send these over the wire to the peer.
            Document[] documentToSendBack = this.Documents.Where(d => ids.Contains(d.Id)).ToArray();

            Log.WriteLine($"IP{this.IP}: Sending {documentToSendBack.Length} documents to IP{peer.IP}. Asked for it: {askedForIt}.");

            // Simulate that the peer receives these documents he asked for.
            peer.OnReceivedDocuments(documentToSendBack, this, askedForIt);
        }
    }
}
