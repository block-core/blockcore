using System;
using System.IO;

namespace NBitcoin.Crypto
{
    public class HashStream : Stream
    {
        public HashStream()
        {

        }

        public override bool CanRead
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanSeek
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanWrite
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int copied = 0;
            int toCopy = 0;
            while(copied != count)
            {
                toCopy = Math.Min(this._Buffer.Length - this._Pos, count - copied);
                Buffer.BlockCopy(buffer, offset + copied, this._Buffer, this._Pos, toCopy);
                copied += (byte)toCopy;
                this._Pos += (byte)toCopy;
                ProcessBlockIfNeeded();
            }
        }


        private byte[] _Buffer = new byte[32];
        private byte _Pos;
        public override void WriteByte(byte value)
        {
            this._Buffer[this._Pos++] = value;
            ProcessBlockIfNeeded();
        }

        private void ProcessBlockIfNeeded()
        {
            if(this._Pos == this._Buffer.Length)
                ProcessBlock();
        }

        private BouncyCastle.Crypto.Digests.Sha256Digest sha = new BouncyCastle.Crypto.Digests.Sha256Digest();
        private void ProcessBlock()
        {
            this.sha.BlockUpdate(this._Buffer, 0, this._Pos);
            this._Pos = 0;
        }

        public uint256 GetHash()
        {
            ProcessBlock();
            this.sha.DoFinal(this._Buffer, 0);
            this._Pos = 32;
            ProcessBlock();
            this.sha.DoFinal(this._Buffer, 0);
            return new uint256(this._Buffer);
        }
    }
}
