using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HashLib;
using NBitcoin;

namespace Blockcore.Networks.XRC.Crypto
{
    internal class XRCHashX11
    {
        private readonly List<IHash> hashers;

        private readonly object hashLock;

        private static readonly Lazy<XRCHashX11> SingletonInstance = new Lazy<XRCHashX11>(LazyThreadSafetyMode.PublicationOnly);

        public XRCHashX11()
        {
            this.hashers = new List<IHash>
            {
                HashFactory.Crypto.SHA3.CreateBlake512(),
                HashFactory.Crypto.SHA3.CreateBlueMidnightWish512(),
                HashFactory.Crypto.SHA3.CreateGroestl512(),
                HashFactory.Crypto.SHA3.CreateSkein512_Custom(),
                HashFactory.Crypto.SHA3.CreateJH512(),
                HashFactory.Crypto.SHA3.CreateKeccak512(),
                HashFactory.Crypto.SHA3.CreateLuffa512(),
                HashFactory.Crypto.SHA3.CreateCubeHash512(),
                HashFactory.Crypto.SHA3.CreateSHAvite3_512_Custom(),
                HashFactory.Crypto.SHA3.CreateSIMD512(),
                HashFactory.Crypto.SHA3.CreateEcho512(),
            };

            this.hashLock = new object();
            this.Multiplier = 1;
        }

        public uint Multiplier { get; private set; }

        /// <summary>
        /// using the instance method is not thread safe.
        /// to calling the hashing method in a multi threaded environment use the create() method
        /// </summary>
        public static XRCHashX11 Instance => SingletonInstance.Value;

        public static XRCHashX11 Create()
        {
            return new XRCHashX11();
        }

        public uint256 Hash(byte[] input)
        {
            var buffer = input;

            lock (this.hashLock)
            {
                List<IHash> hashers = this.hashers;

                foreach (IHash hasher in hashers)
                {
                    buffer = hasher.ComputeBytes(buffer).GetBytes();
                }
            }

            return new uint256(buffer.Take(32).ToArray());
        }
    }
}
