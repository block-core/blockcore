using System;
using DBreeze.Exceptions;

namespace Blockcore.Utilities
{
    public class DBreezeRetryOptions : RetryOptions
    {
        public DBreezeRetryOptions(RetryStrategyType type = RetryStrategyType.Simple)
            : base(1, TimeSpan.FromMilliseconds(100), type, typeof(TableNotOperableException))
        {
        }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public static RetryOptions Default => new DBreezeRetryOptions();
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
    }
}