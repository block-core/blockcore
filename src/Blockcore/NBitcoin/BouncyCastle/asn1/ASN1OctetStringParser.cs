using System.IO;

namespace Blockcore.NBitcoin.BouncyCastle.asn1
{
    internal interface Asn1OctetStringParser
        : IAsn1Convertible
    {
        Stream GetOctetStream();
    }
}
