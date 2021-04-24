using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HashLib;
using NBitcoin;

namespace Blockcore.Networks.XRC.Crypto
{
    public sealed class XRCHashX13
    {
        private readonly List<IHash> hashersV1;
        private readonly List<IHash> hashersV2;

        private readonly object hashLock;

        private static readonly Lazy<XRCHashX13> SingletonInstance = new Lazy<XRCHashX13>(LazyThreadSafetyMode.PublicationOnly);

        public XRCHashX13()
        {
            this.hashersV1 = new List<IHash>
            {
                HashFactory.Crypto.SHA3.CreateBlake512(),
                HashFactory.Crypto.SHA3.CreateBlueMidnightWish512(),
                HashFactory.Crypto.SHA3.CreateGroestl512(),
                HashFactory.Crypto.SHA3.CreateSkein512(),
                HashFactory.Crypto.SHA3.CreateJH512(),
                HashFactory.Crypto.SHA3.CreateKeccak512(),
                HashFactory.Crypto.SHA3.CreateLuffa512(),
                HashFactory.Crypto.SHA3.CreateCubeHash512(),
                HashFactory.Crypto.SHA3.CreateSHAvite3_512(),
                HashFactory.Crypto.SHA3.CreateSIMD512(),
                HashFactory.Crypto.SHA3.CreateEcho512(),
                HashFactory.Crypto.SHA3.CreateHamsi512(),
                HashFactory.Crypto.SHA3.CreateFugue512(),
            };

            this.hashersV2 = new List<IHash>
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
                HashFactory.Crypto.SHA3.CreateHamsi512(),
                HashFactory.Crypto.SHA3.CreateFugue512(),
            };

            this.hashLock = new object();
            this.Multiplier = 1;
        }

        public uint Multiplier { get; private set; }

        /// <summary>
        /// using the instance method is not thread safe.
        /// to calling the hashing method in a multi threaded environment use the create() method
        /// </summary>
        public static XRCHashX13 Instance => SingletonInstance.Value;

        public static XRCHashX13 Create()
        {
            return new XRCHashX13();
        }

        public uint256 Hash(byte[] input, int version)
        {
            var buffer = input;

            lock (this.hashLock)
            {
                List<IHash> hashers = this.hashersV1;
                if (version == 2)
                {
                    hashers = this.hashersV2;
                }

                foreach (IHash hasher in hashers)
                {
                    buffer = hasher.ComputeBytes(buffer).GetBytes();
                }
            }

            return new uint256(buffer.Take(32).ToArray());
        }
    }
}
