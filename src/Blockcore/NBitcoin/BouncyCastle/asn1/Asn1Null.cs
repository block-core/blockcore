namespace NBitcoin.BouncyCastle.Asn1
{
    /**
     * A Null object.
     */
    internal abstract class Asn1Null
        : Asn1Object
    {
        private protected Asn1Null()
        {
        }

        public override string ToString()
        {
            return "NULL";
        }
    }
}
