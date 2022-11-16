using System;
using Blockcore.Features.Wallet.Exceptions;

namespace Blockcore.Features.Wallet.Types
{
    /// <summary>
    /// An indicator of how fast a transaction will be accepted in a block.
    /// </summary>
    public enum FeeType
    {
        /// <summary>
        /// Slow.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Avarage.
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Fast.
        /// </summary>
        High = 105
    }

    public static class FeeParser
    {
        public static FeeType Parse(string value)
        {
            bool isParsed = Enum.TryParse<FeeType>(value, true, out FeeType result);
            if (!isParsed)
            {
                throw new FormatException($"FeeType {value} is not a valid FeeType");
            }

            return result;
        }

        /// <summary>
        /// Map a fee type to the number of confirmations
        /// </summary>
        public static int ToConfirmations(this FeeType fee)
        {
            return fee switch
            {
                FeeType.Low => 50,
                FeeType.Medium => 20,
                FeeType.High => 5,
                _ => throw new WalletException("Invalid fee"),
            };
        }
    }
}
