namespace Blockcore.NBitcoin.BouncyCastle.math.field
{
    internal interface IFiniteField
    {
        BigInteger Characteristic
        {
            get;
        }

        int Dimension
        {
            get;
        }
    }
}
