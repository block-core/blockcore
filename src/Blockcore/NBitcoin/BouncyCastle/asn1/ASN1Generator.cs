using System.IO;

namespace Blockcore.NBitcoin.BouncyCastle.asn1
{
    internal abstract class Asn1Generator
    {
        private Stream _out;

        protected Asn1Generator(
            Stream outStream)
        {
            this._out = outStream;
        }

        protected Stream Out
        {
            get
            {
                return this._out;
            }
        }

        public abstract void AddObject(Asn1Encodable obj);

        public abstract Stream GetRawOutputStream();

        public abstract void Close();
    }
}
