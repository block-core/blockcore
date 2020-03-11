using System;

namespace Blockcore.Features.PoA
{
    public class NotAFederationMemberException : Exception
    {
        public NotAFederationMemberException() : base("Not a federation member!")
        {
        }
    }
}
