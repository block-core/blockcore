namespace NBitcoin.BouncyCastle.Asn1.X9
{
    internal abstract class X9ECParametersHolder
    {
        private X9ECParameters parameters;

        public X9ECParameters Parameters
        {
            get
            {
                X9ECParametersHolder lockObj = this;

                lock(lockObj)
                {
                    if(lockObj.parameters == null)
                    {
                        lockObj.parameters = lockObj.CreateParameters();
                    }

                    return lockObj.parameters;
                }
            }
        }

        protected abstract X9ECParameters CreateParameters();
    }
}
