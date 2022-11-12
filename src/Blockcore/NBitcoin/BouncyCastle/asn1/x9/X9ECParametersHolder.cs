namespace NBitcoin.BouncyCastle.Asn1.X9
{
    internal abstract class X9ECParametersHolder
    {
        private X9ECParameters parameters;

        private readonly object lockObj = new object();

        public X9ECParameters Parameters
        {
            get
            {
                lock(lockObj)
                {
                    if(this.parameters == null)
                    {
                        this.parameters = CreateParameters();
                    }

                    return this.parameters;
                }
            }
        }

        protected abstract X9ECParameters CreateParameters();
    }
}
