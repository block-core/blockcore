using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Networks.Strax.Federation;
using NBitcoin;

namespace Blockcore.Networks.Strax.ScriptTemplates
{
    public class PayToFederationTemplate : ScriptTemplate
    {
        private static readonly PayToFederationTemplate _Instance = new PayToFederationTemplate();

        public static PayToFederationTemplate Instance
        {
            get { return _Instance; }
        }

        public Script GenerateScriptPubKey(FederationId federationId)
        {
            var ops = new List<Op>();
            ops.Add(Op.GetPushOp(federationId.ToBytes()));
            ops.Add(OpcodeType.OP_NOP9); // OP_FEDERATION
            ops.Add(OpcodeType.OP_CHECKMULTISIG);
            return new Script(ops);
        }

        protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
        {
            Op[] ops = scriptPubKeyOps;
            if (ops.Length < 3)
                return false;

            byte[] federationId = ops[0].PushData;

            return ops[1].Code == OpcodeType.OP_NOP9 && ops[2].Code == OpcodeType.OP_CHECKMULTISIG; // OP_FEDERATION
        }

        public PayToMultiSigTemplateParameters ExtractScriptPubKeyParameters(Script scriptPubKey, Network network)
        {
            bool needMoreCheck;
            if (!FastCheckScriptPubKey(scriptPubKey, out needMoreCheck))
                return null;
            Op[] ops = scriptPubKey.ToOps().ToArray();
            if (!CheckScriptPubKeyCore(scriptPubKey, ops))
                return null;

            byte[] federationId = ops[0].PushData;
            (PubKey[] pubKeys, int signatureCount) = ((StraxBaseNetwork)network).Federations.GetFederation(federationId).GetFederationDetails();
            return new PayToMultiSigTemplateParameters() { PubKeys = pubKeys, SignatureCount = signatureCount };
        }

        protected override bool FastCheckScriptSig(Script scriptSig, Script scriptPubKey, out bool needMoreCheck)
        {
            byte[] bytes = scriptSig.ToBytes(true);
            if (bytes.Length == 0 ||
                bytes[0] != (byte)OpcodeType.OP_0)
            {
                needMoreCheck = false;
                return false;
            }

            needMoreCheck = true;
            return true;
        }

        protected override bool CheckScriptSigCore(Network network, Script scriptSig, Op[] scriptSigOps, Script scriptPubKey, Op[] scriptPubKeyOps)
        {
            if (!scriptSig.IsPushOnly)
                return false;
            if (scriptSigOps[0].Code != OpcodeType.OP_0)
                return false;
            if (scriptSigOps.Length == 1)
                return false;
            if (!scriptSigOps.Skip(1).All(s => TransactionSignature.ValidLength(s.PushData.Length) || s.Code == OpcodeType.OP_0))
                return false;

            if (scriptPubKeyOps != null)
            {
                if (!CheckScriptPubKeyCore(scriptPubKey, scriptPubKeyOps))
                    return false;

                (PubKey[] pubKeys, int sigCountExpected) = ((StraxBaseNetwork)network).Federations.GetFederation(scriptPubKeyOps[0].PushData).GetFederationDetails();
                return sigCountExpected == scriptSigOps.Length + 1;
            }

            return true;

        }

        public TransactionSignature[] ExtractScriptSigParameters(Network network, Script scriptSig)
        {
            bool needMoreCheck;
            if (!FastCheckScriptSig(scriptSig, null, out needMoreCheck))
                return null;
            Op[] ops = scriptSig.ToOps().ToArray();
            if (!CheckScriptSigCore(network, scriptSig, ops, null, null))
                return null;
            try
            {
                return ops.Skip(1).Select(i => i.Code == OpcodeType.OP_0 ? null : new TransactionSignature(i.PushData)).ToArray();
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public override TxOutType Type
        {
            // This would be TX_FEDERATION on the original implementation.
            get { return TxOutType.TX_NONSTANDARD; }
        }

        public Script GenerateScriptSig(TransactionSignature[] signatures)
        {
            return GenerateScriptSig((IEnumerable<TransactionSignature>)signatures);
        }

        public Script GenerateScriptSig(IEnumerable<TransactionSignature> signatures)
        {
            var ops = new List<Op>();
            ops.Add(OpcodeType.OP_0);
            foreach (TransactionSignature sig in signatures)
            {
                if (sig == null)
                    ops.Add(OpcodeType.OP_0);
                else
                    ops.Add(Op.GetPushOp(sig.ToBytes()));
            }

            return new Script(ops);
        }
    }
}