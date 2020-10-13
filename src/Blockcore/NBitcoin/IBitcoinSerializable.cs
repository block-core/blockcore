using System;
using System.IO;
using Blockcore.Consensus;
using Blockcore.Consensus.TransactionInfo;
using NBitcoin.Protocol;

namespace NBitcoin
{
    public interface IBitcoinSerializable
    {
        void ReadWrite(BitcoinStream stream);
    }

    public static class BitcoinSerializableExtensions
    {
        public static void ReadWrite(this IBitcoinSerializable serializable, byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                serializable.ReadWrite(new BitcoinStream(stream, false));
            }
        }

        public static void ReadWrite(this IBitcoinSerializable serializable, byte[] bytes, ConsensusFactory consensusFactory)
        {
            if (consensusFactory == null)
                throw new ArgumentException("{0} cannot be null", nameof(consensusFactory));

            using (var stream = new MemoryStream(bytes))
            {
                serializable.ReadWrite(new BitcoinStream(stream, false, consensusFactory));
            }
        }

        public static int GetSerializedSize(this IBitcoinSerializable serializable, ConsensusFactory consensusFactory, SerializationType serializationType)
        {
            var bitcoinStream = new BitcoinStream(Stream.Null, true, consensusFactory)
            {
                Type = serializationType,
            };

            bitcoinStream.ReadWrite(serializable);
            return (int)bitcoinStream.Counter.WrittenBytes;
        }

        public static int GetSerializedSize(this IBitcoinSerializable serializable, ConsensusFactory consensusFactory, uint version, SerializationType serializationType)
        {
            var bitcoinStream = new BitcoinStream(Stream.Null, true, consensusFactory, version)
            {
                Type = serializationType
            };

            bitcoinStream.ReadWrite(serializable);
            return (int)bitcoinStream.Counter.WrittenBytes;
        }

        public static int GetSerializedSize(this IBitcoinSerializable serializable, TransactionOptions options)
        {
            var bitcoinStream = new BitcoinStream(Stream.Null, true)
            {
                TransactionOptions = options
            };

            serializable.ReadWrite(bitcoinStream);
            return (int)bitcoinStream.Counter.WrittenBytes;
        }

        public static string ToHex(this IBitcoinSerializable serializable, ConsensusFactory consensusFactory, SerializationType serializationType = SerializationType.Disk)
        {
            using (var memoryStream = new MemoryStream())
            {
                var bitcoinStream = new BitcoinStream(memoryStream, true, consensusFactory)
                {
                    Type = serializationType,
                };

                bitcoinStream.ReadWrite(serializable);
                memoryStream.Seek(0, SeekOrigin.Begin);
                byte[] bytes = memoryStream.ReadBytes((int)memoryStream.Length);

                return DataEncoders.Encoders.Hex.EncodeData(bytes);
            }
        }

        public static int GetSerializedSize(this IBitcoinSerializable serializable)
        {
            var bitcoinStream = new BitcoinStream(Stream.Null, true)
            {
                Type = SerializationType.Disk,
            };

            bitcoinStream.ReadWrite(serializable);
            return (int)bitcoinStream.Counter.WrittenBytes;
        }

        public static int GetSerializedSize(this IBitcoinSerializable serializable, ConsensusFactory consensusFactory)
        {
            var bitcoinStream = new BitcoinStream(Stream.Null, true, consensusFactory)
            {
                Type = SerializationType.Disk,
            };

            bitcoinStream.ReadWrite(serializable);

            return (int)bitcoinStream.Counter.WrittenBytes;
        }

        public static void FromBytes(this IBitcoinSerializable serializable, byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var bitcoinStream = new BitcoinStream(ms, false);
                serializable.ReadWrite(bitcoinStream);
            }
        }

        public static void FromBytes(this IBitcoinSerializable serializable, byte[] bytes, ConsensusFactory consensusFactory)
        {
            if (consensusFactory == null)
                throw new ArgumentException("{0} cannot be null", nameof(consensusFactory));

            using (var ms = new MemoryStream(bytes))
            {
                var bitcoinStream = new BitcoinStream(ms, false, consensusFactory);

                serializable.ReadWrite(bitcoinStream);
            }
        }

        public static T Clone<T>(this T serializable, ConsensusFactory consensusFactory) where T : IBitcoinSerializable, new()
        {
            T instance = consensusFactory.TryCreateNew<T>();
            if (instance == null)
                instance = new T();

            instance.FromBytes(serializable.ToBytes(consensusFactory), consensusFactory);
            return instance;
        }

        public static byte[] ToBytes(this IBitcoinSerializable serializable)
        {
            using (var ms = new MemoryStream())
            {
                var bms = new BitcoinStream(ms, true);

                serializable.ReadWrite(bms);

                return ToArrayEfficient(ms);
            }
        }

        public static byte[] ToBytes(this IBitcoinSerializable serializable, uint version)
        {
            using (var ms = new MemoryStream())
            {
                var bms = new BitcoinStream(ms, true, version);

                serializable.ReadWrite(bms);

                return ToArrayEfficient(ms);
            }
        }

        public static byte[] ToBytes(this IBitcoinSerializable serializable, ConsensusFactory consensusFactory)
        {
            if (consensusFactory == null)
                throw new ArgumentException("{0} cannot be null", nameof(consensusFactory));

            using (var ms = new MemoryStream())
            {
                var bms = new BitcoinStream(ms, true, consensusFactory);

                serializable.ReadWrite(bms);

                return ToArrayEfficient(ms);
            }
        }

        public static byte[] ToArrayEfficient(this MemoryStream ms)
        {
#if !NETCORE
            var bytes = ms.GetBuffer();
            Array.Resize(ref bytes, (int)ms.Length);
            return bytes;
#else
            return ms.ToArray();
#endif
        }
    }
}