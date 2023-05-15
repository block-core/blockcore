using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.NBitcoin;
using Blockcore.Networks.Strax.ScriptTemplates;

namespace Blockcore.Networks.Strax.Federation
{
    public interface IFederation
    {
        Script MultisigScript { get; }
        FederationId Id { get; }

        (PubKey[] transactionSigningKeys, int signaturesRequired) GetFederationDetails();
    }

    /// <summary>
    /// Compares two byte arrays for equality.
    /// </summary>
    public sealed class ByteArrayComparer : IEqualityComparer<byte[]>, IComparer<byte[]>
    {
        public int Compare(byte[] first, byte[] second)
        {
            int firstLen = first?.Length ?? -1;
            int secondLen = second?.Length ?? -1;
            int commonLen = Math.Min(firstLen, secondLen);

            for (int i = 0; i < commonLen; i++)
            {
                if (first[i] == second[i])
                    continue;

                return (first[i] < second[i]) ? -1 : 1;
            }

            return firstLen.CompareTo(secondLen);
        }

        public bool Equals(byte[] first, byte[] second)
        {
            return this.Compare(first, second) == 0;
        }

        public int GetHashCode(byte[] obj)
        {
            ulong hash = 17;

            foreach (byte objByte in obj)
            {
                hash = (hash << 5) - hash + objByte;
            }

            return (int)hash;
        }
    }

    public class FederationId : IBitcoinSerializable
    {
        byte[] federationId;
        ByteArrayComparer comparer;

        public FederationId()
        {
        }

        public FederationId(byte[] value)
        {
            this.federationId = value;
            this.comparer = new ByteArrayComparer();
        }

        public void ReadWrite(BitcoinStream s)
        {
            s.ReadWrite(ref this.federationId);
        }

        public override bool Equals(object obj)
        {
            return this.comparer.Equals(((FederationId)obj).federationId, this.federationId);
        }

        public override int GetHashCode()
        {
            return this.comparer.GetHashCode(this.federationId);
        }
    }

    public class Federation : IFederation
    {
        private PubKey[] transactionSigningKeys;

        private int signaturesRequired;

        public Script MultisigScript { get; private set; }

        public FederationId Id { get; private set; }

        /// <summary>
        /// Creates a new federation from a set of transaction signing keys.
        /// </summary>
        /// <param name="transactionSigningPubKeys">A list of transaction signing PubKeys.</param>
        /// <param name="signaturesRequired">The amount of signatures required to ensure that the transaction is fully signed.</param>
        public Federation(IEnumerable<PubKey> transactionSigningPubKeys, int? signaturesRequired = null)
        {
            // Ensures that the federation id will always map to the same members in the same order.
            this.transactionSigningKeys = transactionSigningPubKeys.OrderBy(k => k.ToHex()).ToArray();
            this.signaturesRequired = signaturesRequired ?? (this.transactionSigningKeys.Length + 1) / 2;

            // The federationId is derived by XOR'ing all the genesis federation members.
            byte[] federationId = this.transactionSigningKeys.First().ToBytes();
            foreach (PubKey pubKey in this.transactionSigningKeys.Skip(1))
            {
                byte[] pubKeyBytes = pubKey.ToBytes();
                for (int i = 0; i < federationId.Length; i++)
                    federationId[i] ^= pubKeyBytes[i];
            }

            this.Id = new FederationId(federationId);
            this.MultisigScript = PayToFederationTemplate.Instance.GenerateScriptPubKey(this.Id);
        }

        public (PubKey[] transactionSigningKeys, int signaturesRequired) GetFederationDetails()
        {
            // Until dynamic membership is implemented we just return the genesis members.
            return (this.transactionSigningKeys, this.signaturesRequired);
        }
    }

    public interface IFederations
    {
        /// <summary>
        /// Registers a new federation with transaction signing keys.
        /// </summary>
        /// <param name="federation">The federation to be registered.</param>
        void RegisterFederation(IFederation federation);

        IFederation GetFederation(FederationId federationId);

        IFederation GetFederation(byte[] federationId);

        IFederation GetOnlyFederation();
    }

    public class Federations : IFederations
    {
        private readonly Dictionary<FederationId, IFederation> federations;

        public Federations()
        {
            this.federations = new Dictionary<FederationId, IFederation>();
        }

        public IFederation GetFederation(FederationId federationId)
        {
            return this.federations.TryGetValue(federationId, out IFederation federation) ? federation : null;
        }

        public IFederation GetFederation(byte[] federationId)
        {
            return this.federations.TryGetValue(new FederationId(federationId), out IFederation federation) ? federation : null;
        }

        // TODO: Deprectate this method when multiple federations are supported.
        public IFederation GetOnlyFederation()
        {
            return this.federations.First().Value;
        }

        public void RegisterFederation(IFederation federation)
        {
            // TODO: Remove this when multiple federations are supported.
            this.federations.Clear();

            this.federations[federation.Id] = federation;
        }
    }
}